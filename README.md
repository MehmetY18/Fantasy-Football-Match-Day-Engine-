# Fantasy-Football-Match-Day-Engine

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?logo=rabbitmq&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)


---
## 🇹🇷 Türkçe (TR)

### Mimari Kararlar (Architectural Decisions)

#### 1. ASP.NET Core Web API ve Arka Plan Servisleri (Hosted Services)
Sistem canlı maç günlerinde yüksek yük altında çalışacağı için, kullanıcıların istek attığı API katmanı ile arka planda verileri işleyen katmanları birbirinden ayırdık. API katmanı (`MatchApi`) istekleri karşılayıp skor dönerken, arka planda çalışan Worker servisleri veritabanı ve kuyruk işlemlerini bağımsız olarak yürütür.

#### 2. RabbitMQ & MassTransit Entegrasyonu
Mikroservislerin birbiriyle asenkron haberleşebilmesi ve fantezi futbol olay zincirinin (Event Chain) tıkır tıkır ilerleyebilmesi için mesaj kuyruğu olarak **RabbitMQ** kullanılmıştır.  
**MassTransit** ise bu yapıyı yönetirken şu kurumsal çözümleri sağlamak için seçilmiştir:
- Veritabanı ile mesaj kuyruğunu tek bir transaction halinde bağlayarak veri kaybını sıfıra indiren **Transactional Outbox Pattern** konfigürasyonu için kullanıldı.
- Tüm consumer kuyruklarını otomatik oluşturan `ConfigureEndpoints` mekanizması için tercih edildi.

#### 3. Redis Sorted Set ve Hash Yapıları
Milyonlarca kullanıcının anlık olarak fantezi puanlarının hesaplandığı ve sürekli yenilenen liderlik tablosunda (Leaderboard) ilişkisel bir veritabanına sorgu atmak sistem kilitlemelerine (deadlock) yol açar. Bu performansı maksimize etmek için Redis'in yerleşik sıralama algoritmasına sahip **Sorted Set (`ZADD / ZRANK / ZRANGE`)** yapısı ve kullanıcı adı önbelleklemesi için **Redis Hash** yapısı tercih edilmiştir.

#### 4. Bağımlılık ve Veritabanı Yönetimi (`IServiceScopeFactory`)
Arka plandaki Worker servislerinin veritabanı bağlamı (`AppDbContext`) ile çakışmasını engellemek, bellek sızıntılarına yol açmamak ve işlemlerin güvenli (thread-safe) yürümesini sağlamak adına her periyotta `IServiceScopeFactory` kullanılarak izole bir scope yaratılmıştır.

### 5. PostgreSQL ve Entity Framework Core
Uygulamanın ana veri depolama katmanında ilişkisel veritabanı (RDBMS) standartlarına ve ACID (Tutarlılık ve Güvenilirlik) garantisine tam uymak adına **PostgreSQL** tercih edilmiştir. Mock JSON dosyalarından akan verilerin ilişkisel tablolara (User, Team, Squad, SquadPlayer, Player, PlayerStats, Match, MatchScore, Fixture) normalize edilerek dağıtılması sağlanmıştır.

Veri erişim katmanında (ORM) ise **Entity Framework Core** kullanılmıştır. EF Core bize şu kurumsal avantajları sağlamıştır:
- **Code-First ve Geçiş Yönetimi:** Veritabanı şemasını C# sınıfları üzerinden türetip, `Migrations` mekanizmasıyla veritabanı yönetimini kolaylaştırdı.
- **Sorgu Optimizasyonu (Performans):** Kadro verilerini çekerken `.Include(s => s.Players)` operasyonlarıyla ilişkili tablolar tek seferde yüklenmiştir. Ayrıca, Redis önbelleğinde bulunamayan verileri veritabanından çekerken veya kadro toplam puanlarını hesaplarken LINQ `.Contains()` yapısı kullanılmıştır. Bu sayede PostgreSQL arkada toplu sorgu (`WHERE id = ANY (@array)`) işleterek 15 farklı oyuncu için veritabanına 15 kere döngüyle gitmek yerine tek bir sorguyla işi bitirmiş, sinsi N+1 veritabanı yükünü tamamen engellemiştir.

#### 6. Docker & Docker Compose ile Altyapı İzolasyonu
PostgreSQL, Redis ve RabbitMQ gibi harici bağımlılıkların geliştirici bilgisayarından bağımsız, izole ve tekrarlanabilir (reproducible) bir ortamda çalışması için konteyner mimarisi (Docker) tercih edilmiştir. `docker-compose.yml` dosyası sayesinde tüm veri depoları ve mesaj kuyrukları tek bir komutla (`docker-compose up -d`) başlatılabilir.

---

### Mimari Trade-Offs (Architectural Trade-offs)

#### 1. Redis Liderlik Tablosu
* **Verilen Kararlar:** Liderlik tablosu ($O(1)$ sayfalama için) ve kullanıcı profil bilgileri Redis in-memory veri yapısında (ZSET ve Hash) tutuldu.
* **Trade Off (Maliyet/Kayıp):** Bellek (RAM) maliyeti arttı. On binlerce aktif kullanıcının skorları ve isimleri bellek kaplar.
* **Neden Tercih Edildi:** PostgreSQL üzerinde her kullanıcı isteğinde `ORDER BY TotalPoints DESC` çalıştırmak veritabanı CPU'sunu kilitlerdi. Yüksek okuma hızını korumak için bellek maliyeti üstlenildi.

#### 2. Önbellek Kenara Alma Modeli ve Önbellek Geçersizleştirme Karmaşıklığı
* **Verilen Kararlar:** Kullanıcı bilgileri önce Redis'ten sorgulanır, yoksa PostgreSQL'e gidilip önbellek ısıtılır (Cache Warming).
* **Trade Off (Maliyet/Kayıp):** Veri tutarlılığı (Eventual Consistency) riski. Bir kullanıcı adını değiştirdiğinde, Redis'teki cache hemen güncellenmezse veya TTL (Time-To-Live) süresi dolmazsa liderlik tablosunda eski isim görünebilir.
* **Neden Tercih Edildi:** Veritabanına atılacak binlerce okuma sorgusunu engellemek, milisaniyelik veri gecikmesi kabulünden çok daha değerlidir.

#### 3. Veri Erişim Katmanı
* **Verilen Kararlar:** Veritabanı işlemleri için Entity Framework Core tercih edildi.
* **Trade Off (Maliyet/Kayıp):** Hafif bir çalışma zamanı (runtime) overhead'i ve Dapper/Raw SQL'e kıyasla mikro saniyelik performans kaybı.
* **Neden Tercih Edildi:** LINQ ile tip güvenliği (Type Safety), Migration yönetimi ve `.Select()` ile sadece gereken kolonları çekebilme esnekliği, kodun sürdürülebilirliğini ve geliştirme hızını ciddi şekilde artırdı.

#### 4. Outbox Pattern
* **Verilen Kararlar:** Event publish güvenirliği için kullanıldı.
* **Trade Off (Maliyet/Kayıp):** Veritabanına ekstradan bir `OutboxState` tablosu yükü getirdik.
* **Neden Tercih Edildi:** Veritabanına oyuncuyu yazıp RabbitMQ'ya mesaj atarken event'ler kaybolabilirdi bunun olmaması için kullanıldı.

#### 5. Zarf (Envelope) ve REST Mimarisi
* **Verilen Kararlar:** Tüm istekleri devasa payload alanları ve POST metoduyla göndermek yerine; HTTP metotlarına (GET, POST, DELETE), route parametrelerine ve Query String'lere dayalı standart RESTful API mimarisine uygun olarak geliştirilmiştir.
* **Trade Off (Maliyet/Kayıp):**
  1. **URL Uzunluk Sınırı (URL Truncation) Riski:** Gelecekte eklenebilecek çok karmaşık ve devasa filtre senaryolarında HTTP protokolünün URL uzunluk sınırlarına (genelde 2048 karakter) takılma riski ve maliyeti kabul edilmiştir.
  2. **Protokol Bağımlılığı (Protocol Coupling):** İzleme verilerinin HTTP Header'larına kurgulanması mimariyi HTTP'ye bağlar. İleride WebSocket veya SignalR gibi çift yönlü (duplex) hatlara geçildiğinde bu izleme verilerini taşımak için özel bir zarf katmanı yazılması gerekecektir.
* **Neden Tercih Edildi:** Band genişliği (bandwidth) tasarrufu sağlamak, `GET` isteklerinin CDN/Browser düzeyinde önbelleklenmesini (caching) mümkün kılmak ve API'yi kendini açıklayan (self-describing) standart bir yapıya kavuşturmak için.

#### 6. Konteynerleştirme ve Yerel Kaynak Tüketimi
* **Verilen Kararlar:** Tüm bağımlılıklar (PostgreSQL, Redis, RabbitMQ) Docker konteynerları üzerinde izole olarak kurgulandı.
* **Trade Off (Maliyet/Kayıp):** Geliştirme (Local Development) aşamasında RAM ve CPU gibi sistem kaynaklarında ek overhead (Docker daemon yükü).
* **Neden Tercih Edildi:** Test/CI-CD ortamlarında projeyi sıfır kurulum maliyetiyle ayağa kaldırabilmek için bu maliyet kabul edilmiştir.

---
---

##  🇺🇸 English (EN)

### Architectural Decisions

#### 1. ASP.NET Core Web API and Background Services (Hosted Services)
Since the system is designed to run under heavy concurrent load during live match days, the client-facing API layer has been completely decoupled from the background data processing layers. The API layer (`MatchApi`) handles incoming traffic and serves data, while the independent Worker services (Hosted Services) execute database and message queue operations asynchronously.

#### 2. RabbitMQ & MassTransit Integration
**RabbitMQ** is utilized as the message broker to enable asynchronous communication between microservices and ensure a resilient event chain lifecycle.  
**MassTransit** was chosen as the abstraction layer to implement the following enterprise patterns:
- **Transactional Outbox Pattern:** Configured to bind database writes and event publishing into a single atomic transaction, reducing the risk of data loss to zero.
- **Automatic Endpoints Configuration:** Leveraged via `ConfigureEndpoints` to enforce type-safe automatic exchange-queue bindings, eliminating manual naming errors.

#### 3. Redis Sorted Set and Hash Structures
Executing heavy `ORDER BY` queries on a relational database for a highly volatile leaderboard with millions of active users triggers severe database lockups (deadlocks). To maximize read throughput and sorting performance, Redis In-Memory structures were implemented:
- **Sorted Set (`ZADD / ZRANK / ZRANGE`):** Utilized to compute real-time user point rankings and paginated score profiles within millisecond response times.
- **Redis Hash:** Employed for a Cache-Aside username lookup engine, bypassing the relational database entirely during high-volume reads.

#### 4. Dependency and Database Scope Management (`IServiceScopeFactory`)
Since background tasks run within a Singleton lifespan, directly injecting a Scoped database context (`AppDbContext`) introduces severe memory leaks and thread-safety violations. To prevent this architectural risk, an isolated short-lived scope is explicitly generated via `IServiceScopeFactory` during each execution interval, ensuring thread-safe database interactions.

### 5. PostgreSQL and Entity Framework Core
**PostgreSQL** was selected as the primary relational persistence layer to enforce absolute data integrity and ACID guarantees across normalized tables (Users, Squads, Players, SquadPlayers). 

**EF Core** was enforced as the Object-Relational Mapper (ORM) providing distinct corporate advantages:
- **Code-First & Migration Pipelines:** Managed database schemas directly through C# classes, ensuring automated deployment controls.
- **Query Optimization & Performance:** Eager loading via `.Include(s => s.Players)` was implemented to fetch multi-level relational data efficiently. Furthermore, during cache misses or total score calculations, the LINQ `.Contains()` pattern was leveraged. This prompts PostgreSQL to execute a single high-performance batch evaluation under the hood (`WHERE id = ANY (@array)`), effectively bypassing heavy iterative loops for 15 individual players and completely eliminating the N+1 database read bottleneck.


#### 6. Containerized Infrastructure with Docker & Docker Compose
All external infrastructure components required by the system, including PostgreSQL, Redis, and RabbitMQ, are provisioned via the `docker-compose.yml` file in the root directory and can be launched instantly with a single command (`docker-compose up -d`).

---

### Architectural Trade-offs

#### 1. Redis Leaderboard RAM Footprint
- **Decision:** The application offloads real-time leaderboard pagination ($O(1)$ complexity) and manager profiles to Redis in-memory data structures (ZSET and Hashes).
- **Trade-off (Cons):** Elevated Memory (RAM) overhead. Maintaining scores and usernames for tens of thousands of active managers increases cloud hosting costs.
- **Justification:** Executing `ORDER BY TotalPoints DESC` in PostgreSQL for every incoming read request would eventually exhaust database CPU resources. Volatile memory cost was embraced to guarantee ultra-low latency reads.

#### 2. Cache-Aside Model & Cache Invalidation Complexity
- **Decision:** Manager metadata is prioritized via Redis queries; if a cache miss occurs, the system triggers a fallback query to PostgreSQL to warm the cache dynamically.
- **Trade-off (Cons):** Risk of temporary data inconsistency (**Eventual Consistency**). If a manager updates their profile, the change might not immediately reflect on the leaderboard until the cache is explicitly invalidated or its TTL expires.
- **Justification:** Suppressing thousands of redundant read queries to the core database outweighs the minor cost of a millisecond-level data propagation delay.

#### 3. Data Access Abstraction Layer
- **Decision:** Entity Framework Core was enforced as the primary data access driver over raw SQL or lightweight micro-ORMs like Dapper.
- **Trade-off (Cons):** A minor runtime abstraction overhead and a microsecond-level performance trade-off compared to raw native SQL execution.
- **Justification:** LINQ-driven Type Safety, automated Migration pipelines, and the structural flexibility of projecting only requested columns via `.Select()` significantly increased code maintainability and development velocity.

#### 4. Transactional Outbox Pattern Overhead
- **Decision:** Enforced to guarantee event publishing resilience across distributed boundary lines.
- **Trade-off (Cons):** Introduces a continuous storage and I/O write overhead by provisioning and scanning an auxiliary `OutboxState` log table inside the relational database.
- **Justification:** Publishing events directly to RabbitMQ during a database write transaction introduces split-brain risks if network partitions occur. Sacrificing an extra database write step ensures absolute atomicity and zero message loss.

#### 5. API Boundary: RESTful Design vs. Strict Message Envelopes
- **Decision:** Shifted from bulky POST-body RPC messages to a clean, resource-oriented Saf RESTful architecture utilizing standard HTTP verbs (GET, POST, DELETE), path parameters, and Query Strings.
- **Trade-off (Cons):**
  1. **URL Truncation Risks:** Since filtering structures are decoupled from the request payload body and mapped onto URL Query Strings, highly complex reporting queries accept the architectural risk of hitting maximum HTTP URL character length thresholds (typically 2048 characters).
  2. **Protocol Coupling:** Passing tracking parameters through specific HTTP Header bindings maps the current schema directly onto stateless HTTP behaviors. Moving towards full-duplex persistent streams (like WebSockets or SignalR) down the line will require a custom metadata envelope migration layer.
- **Justification:** Optimizes network bandwidth, enables CDN/Browser HTTP caching for `GET` endpoints, and brings self-describing resource standards to the entire API layer.

#### 6. Containerization & Local Resource Overhead
- **Decision:** All core backing services (PostgreSQL, Redis, RabbitMQ) are fully containerized using Docker Compose.
- **Trade-off (Cons):** Introduces minor CPU and RAM execution overhead on local developer environments running the Docker Daemon.
- **Justification:** Accepted to completely eliminate environment drift and to provision the project in test/CI-CD environments with zero setup cost.
