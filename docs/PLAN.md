# CinePick Uygulama Planı

## 1. Amaç ve başarı ölçütü

CinePick; Türkiye'deki vizyon filmlerini, sinemaları ve gerçekten erişilebilir seansları listeleyen, kullanıcının doğal dildeki talebini doğrulanmış filtrelere dönüştüren ve yalnızca mevcut adaylar arasından en fazla üç öneri sunan mobil öncelikli bir web uygulamasıdır.

MVP aşağıdaki uçtan uca akış çalıştığında tamamlanmış sayılır:

1. Kullanıcı konumunu paylaşır veya şehir/ilçe seçer.
2. Doğal dilde film talebini girer.
3. Talep yapılandırılmış ve doğrulanmış bir filtreye dönüştürülür.
4. Zorunlu koşullar SQL ve uygulama koduyla uygulanır.
5. En fazla 20 gerçek film-seans adayı AI ile sıralanır; AI yoksa deterministik algoritma devreye girer.
6. Kullanıcı en fazla üç açıklanabilir öneri görür ve güvenli bilet bağlantısına ilerleyebilir.
7. Yönetici sinema, salon ve seansları manuel yönetebilir.

## 2. Mevcut durum

- Repository boş bir Git deposudur; solution, uygulama kodu, test, CI veya dokümantasyon yoktur.
- `AGENTS.md` bulunmamaktadır; Milestone 1'de oluşturulacaktır.
- Proje greenfield olarak kurulacaktır.
- API anahtarı gerektiren tüm entegrasyonlar mock/fake sağlayıcılarla değiştirilebilir olacaktır.

## 3. Kapsam

### MVP kapsamı

- Film kataloğu, arama, filtreleme ve detay
- Sinema, salon, seans ve yakınlık sorguları
- Mock film ve seans sağlayıcıları; opsiyonel TMDb sağlayıcısı
- Doğal dil ayrıştırma, kesin filtreleme, AI sıralama ve deterministik fallback
- Kimlik doğrulama, tercihler, favoriler, izlenen filmler ve puanlama
- Yönetim panelinden film/sinema/salon/seans ve senkronizasyon yönetimi
- PWA, responsive ve WCAG AA odaklı arayüz
- Docker Compose, gözlemlenebilirlik, güvenlik kontrolleri ve test otomasyonu

### MVP sonrası

- Çok katılımcılı grup önerisi kullanıcı deneyimi
- Lisanslı gerçek seans/bilet sağlayıcısı entegrasyonu
- Gelişmiş harita sağlayıcısı ve rota süresi
- Redis tabanlı dağıtık cache ve çok örnekli çalışma ihtiyaçları
- Gelişmiş davranışsal kişiselleştirme

## 4. Karar kapıları ve açık gereksinimler

Uygulamaya başlamadan önce aşağıdaki kararlar netleştirilmelidir. Bunlar için varsayılan öneri verilmiştir ancak ürün veya güvenlik etkisi olan seçimler onaylanmadan kalıcı biçimde uygulanmamalıdır.

| Konu | Önerilen varsayılan | Neden / etkisi |
|---|---|---|
| Kimlik doğrulama | Mevcut yapıda yoktur. Milestone 1–4 anonim/mock akışla çalışır; Milestone 5'te ASP.NET Core Identity tabanı ve güvenli browser oturumu eklenir | Erken aşamaları auth kararına kilitlemez; nihai SPA güvenlik modeli dağıtım topolojisiyle birlikte kararlaştırılır |
| AI sağlayıcısı | Sağlayıcı bağımsız adapter; ilk gerçek adapter OpenAI-compatible Responses API, varsayılan mock | Anahtar olmadan geliştirme ve test devam eder |
| Arka plan işleri | Quartz.NET | Açık kaynak ve uygulama içine gömülü zamanlama için yeterli; kalıcı job dashboard'u istenirse Hangfire seçilir |
| Harita | MVP'de liste + basit harita adapter arayüzü; sağlayıcı seçimi ertelenir | Harita lisansı, API anahtarı ve maliyet kararı gerekir |
| Bilet bağlantıları | Seed/mock allowlist; gerçek domainler sağlayıcı sözleşmesiyle eklenir | Open redirect ve kötü amaçlı URL riskini azaltır |
| Yaş derecelendirmesi | Uygulama içi normalize enum + kaynak metni | Türkiye sınıflandırmasının sağlayıcılar arası eşlemesi tanımlanmalıdır |
| Para birimi | MVP'de TRY, modelde ISO 4217 kodu | İleride çoklu para birimini engellemez |
| Yerelleştirme | İlk sürüm `tr-TR`, altyapı `en` eklenebilir biçimde | Metin ve hata sözleşmelerini etkiler |
| Kullanıcı verisi saklama | Minimum veri, silme akışı ve kısa süreli öneri girdisi | KVKK kapsamı ve retention süresi ürün kararıdır |
| CI sağlayıcısı | GitHub Actions | Repository barındırma tercihi farklıysa değiştirilir |

İlk uygulama aşamasında hedef barındırma ortamı ve gerçek AI sağlayıcısı açık kalabilir; mock mod ve yerel Docker topolojisiyle geri döndürülebilir şekilde ilerlenir. Kimlik doğrulama Milestone 5'e kadar kapsam dışıdır; o aşamaya geçmeden önce browser oturumu/JWT kararı kesinleştirilir.

## 5. Mimari

### 5.1 Genel yaklaşım

Sistem mikroservis yerine modüler monolit olarak kurulacaktır. Tek deploy edilebilir backend içinde modül sınırları korunur; modüller arası çağrılar Application sözleşmeleri üzerinden yapılır. Veritabanı başlangıçta tek SQL Server veritabanıdır. Şema sahipliği ve bağımlılık kuralları architecture testleriyle korunur.

```text
Angular PWA
    |
    | HTTPS / JSON
    v
ASP.NET Core Minimal API
    |
    +-- Identity / UserPreferences
    +-- Movies / Cinemas / Showtimes
    +-- Recommendations
    +-- Administration
    +-- ExternalProviders
    |
    +--> SQL Server
    +--> Movie metadata provider (Mock veya TMDb)
    +--> Showtime provider (MVP'de Mock)
    +--> AI provider (Mock/Fallback veya yapılandırılmış gerçek adapter)
```

### 5.2 Katmanlar ve bağımlılık yönü

- **Domain:** Entity, value object, domain kuralı ve provider bağımsız kavramlar. EF, HTTP veya AI SDK bağımlılığı içermez.
- **Application:** Vertical slice use-case'leri, request/response modelleri, doğrulama, `Result<T>`, port/interface'ler ve orchestration.
- **Infrastructure:** EF Core, SQL Server, Identity persistence, dış sağlayıcı adapter'ları, resilience, cache ve job uygulamaları.
- **API:** Minimal API endpointleri, auth policy, Problem Details, rate limit, OpenAPI ve composition root.
- **Frontend:** Route tabanlı feature alanları, standalone components, signals ve API istemcileri.

Önerilen yapı:

```text
src/
  backend/
    CinePick.Domain/
    CinePick.Application/
    CinePick.Infrastructure/
    CinePick.Api/
  frontend/
    cinepick-web/
tests/
  CinePick.UnitTests/
  CinePick.IntegrationTests/
  CinePick.ArchitectureTests/
  e2e/
docs/
  DECISIONS/
```

### 5.3 Backend modülleri

- **Identity:** Kullanıcı, rol, oturum ve yönetici yetkisi.
- **Movies:** Film, tür, kişi/kadro, katalog sorguları ve metadata eşleme.
- **Cinemas:** Şehir, ilçe, sinema, salon ve koordinatlar.
- **Showtimes:** Seans, fiyat, dil, format, iptal ve bilet bağlantısı.
- **Recommendations:** İstek ayrıştırma, filtreleme, aday üretimi, sıralama, fallback ve sonuç geçmişi.
- **UserPreferences:** Tercih, favori, izlendi bilgisi ve puanlar.
- **Administration:** CRUD, manuel sync, provider durumu ve sync logları.
- **ExternalProviders:** Film, seans ve AI adapter'ları; domain'e sağlayıcı tipi sızdırmaz.

## 6. Veri modeli ve kalıcılık planı

İlk migration; talepteki entity'leri kapsayacak, ancak kullanım senaryosu olmayan alanlar gereksiz davranışla şişirilmeyecektir. Temel ilkeler:

- Yerel anahtarlar `Guid` veya SQL Server için sıralı UUID stratejisiyle üretilir; harici kimlikler ayrı kolonlarda tutulur.
- Harici kayıtlarda `(ProviderId, ExternalId)` benzersiz indeksleri bulunur.
- Tüm zamanlar UTC saklanır; API ISO 8601 offset/UTC döndürür, Angular `Europe/Istanbul` gösterir.
- Para `decimal` + ISO para birimi koduyla tutulur.
- Liste sorguları DTO projection ve `AsNoTracking` kullanır.
- Seanslarda sorgu desenlerine göre bileşik indeksler planlanır: `(MovieId, StartsAt, IsCancelled)`, `(CinemaId, StartsAt, IsCancelled)` ve provider external key.
- Öneri oturumu; normalize edilmiş filtreyi, aday kimliklerini, kullanılan yöntem/provider bilgisini ve sonuçları audit edilebilir fakat kişisel metni varsayılan olarak saklamayan biçimde tutar.
- Konum ilk sürümde latitude/longitude ve Haversine ile değerlendirilir. SQL spatial tipe geçiş ayrı ADR gerektirir.

Seed veri en az 20 film, 8 tür, 3 şehir, şehir başına 3 sinema, sinema başına 3 salon ve 7 günlük çeşitli seans içerir. Posterler yerel telifli içerik yerine lisanslı placeholder veya üretilmiş mock varlık kullanır.

## 7. Öneri veri akışı

1. API metni, opsiyonel koordinatı ve kullanıcı bağlamını alır; boyut/rate limit uygular.
2. `IMovieRecommendationAssistant` metni tanımlı JSON şemasına göre `RecommendationFilter` modeline dönüştürür.
3. JSON parse, şema ve FluentValidation kontrollerinden geçer. Şehir/tarih gibi kritik belirsizliklerde güvenli varsayılan veya kullanıcıya açıklanabilir validation sonucu üretilir.
4. SQL; tarih, konum kapsamı, seans zamanı, süre, tür, dil, fiyat ve yaş kurallarını kesin olarak filtreler.
5. Uygulama Haversine gibi veritabanında taşınması gerekmeyen son hesapları yapar ve en fazla 20 adayı belirler.
6. AI yalnızca sunucunun oluşturduğu aday DTO'larını sıralar. Serbest URL veya veri erişimi verilmez.
7. Dönen `movieId`/`showtimeId` çiftleri aday kümesine karşı doğrulanır; tekrarlar, eksikler ve sınır dışı puanlar reddedilir.
8. AI timeout, geçersiz çıktı veya servis hatasında yapılandırılabilir ağırlıklı deterministik puanlama çalışır.
9. En fazla üç sonuç; neden, dezavantaj, eşleşen tercih ve puanla kaydedilip döndürülür.

Fallback ağırlıkları başlangıçta tür %25, zaman %20, geçmiş %20, film puanı %15, mesafe %10, fiyat %5, dil/format %5'tir. Normalize fonksiyonları ve tie-break sırası sabitlenerek aynı girdiye aynı sonuç garanti edilir.

## 8. API ve hata sözleşmesi

- Kaynak yolları küçük harfli, çoğul ve kebab-case olacaktır.
- Liste endpointlerinde ortak ama sınırlı pagination/sort/filter sözleşmesi ve maksimum sayfa boyutu uygulanır.
- Entity doğrudan dönmez; endpoint modelleri ile Application DTO'ları ayrılır.
- Application `Result<T>` hata türleri: Validation, NotFound, Conflict, Unauthorized, Forbidden, ExternalService, RateLimit ve Unexpected.
- Tek `ToHttpResult` eşlemesi uygun HTTP kodu ve RFC Problem Details üretir.
- Problem Details gerektiğinde `errorCode`, `traceId` ve alan bazlı `validationErrors` içerir.
- OpenAPI sözleşmesi frontend istemci üretimi veya sözleşme kontrolü için CI'da doğrulanır.
- V1 uyumluluk stratejisi ilk public dağıtımdan önce ADR ile sabitlenir; başlangıçta `/api/...` yolları korunur.

## 9. Dış servisler ve dayanıklılık

- `IMovieMetadataProvider`, `IShowtimeProvider` ve `IMovieRecommendationAssistant` ayrı portlardır.
- Her portun gerçek ve mock implementasyonu aynı contract testlerinden geçer.
- Typed `HttpClient`, timeout, kontrollü retry, circuit breaker ve cancellation kullanılır. Mutating çağrılar körlemesine retry edilmez.
- Senkronizasyon idempotent upsert ve benzersiz external-key constraint ile güvenceye alınır.
- Başarı/başarısızlık, süre ve özet `ExternalSyncLog` içinde tutulur; hassas payload tutulmaz.
- TMDb atıf/lisans bilgisi sağlayıcı dokümantasyonuna göre UI ve `DATA-PROVIDERS.md` içinde gösterilir.
- İzinsiz scraping yapılmaz; gerçek seans sağlayıcısı sözleşme/lisans olmadan eklenmez.

## 10. Güvenlik ve gizlilik planı

- Secret'lar User Secrets veya environment variable üzerinden gelir; `.env.example` yalnızca sahte değerler içerir.
- Admin endpointleri role/policy ile korunur; her hassas use-case sunucu tarafında yetki kontrolü yapar.
- Global ve özellikle öneri/auth endpointlerine ayrı rate limit uygulanır.
- Metin ve JSON istek boyutları sınırlandırılır; tüm girdiler doğrulanır.
- `TicketUrl` yalnızca HTTPS ve yapılandırılmış host allowlist ile kabul edilir; yönlendirme hedefi kullanıcı girdisinden doğrudan üretilmez.
- AI girdisi talimat/veri ayrımıyla kurulur; modelin her alanı güvensiz kabul edilip yeniden doğrulanır.
- Kullanıcı mesajının tamamı ve kesin koordinat varsayılan log/telemetry'ye yazılmaz.
- Hesap/veri silme, retention ve KVKK metni ürün kararıyla tamamlanır.
- Dependency, secret ve container taramaları CI kalite kapısına eklenir.

## 11. Gözlemlenebilirlik ve operasyon

- Serilog ile yapılandırılmış log ve her istekte trace/correlation id.
- OpenTelemetry ile HTTP, EF Core ve dış servis trace'leri; yapılandırılabilir exporter.
- Ölçümler: dış servis/AI süreleri, AI kullanım/token bilgisi varsa güvenli sayaçlar, fallback oranı, cache hit oranı, sync sonucu ve endpoint hata oranı.
- `/health/live` yalnızca süreç canlılığını; `/health/ready` SQL ve kritik bağımlılık hazır olma durumunu gösterir.
- Docker healthcheck'leri readiness ile uyumlu olur; uygulama startup'ta dış API anahtarı yok diye başarısız olmaz.

## 12. Frontend planı

- Angular 22 standalone mimarisi; feature route'ları: home, movies, cinemas, recommendations, profile, admin.
- Signals yerel/UI state için; Reactive Forms varsayılan form yaklaşımıdır. Signal Forms kararı kararlılık ve ekip tercihiyle ayrıca alınır.
- Angular Material üzerine CinePick koyu tema token'ları; yüksek kontrast, klavye erişimi ve görünür focus.
- API state'lerinde loading skeleton, empty, error/retry ve başarı halleri açıkça modellenir.
- Konum izni yalnızca kullanıcı eylemiyle istenir; ret halinde tekrar zorlanmadan şehir/ilçe seçimi sunulur.
- Posterler lazy-load edilir ve fallback görseli kullanır.
- PWA app-shell ve statik asset cache'i uygular; zaman duyarlı seans/API yanıtları stale veri göstermeyecek stratejiyle cache'lenir.
- Admin route'u rol guard kullanır; ancak gerçek yetkilendirme daima API'dedir.

## 13. Aşamalı uygulama planı

Her aşamanın sonunda build, ilgili testler ve kısa güvenlik kontrolü çalıştırılır; hata varken sonraki aşamaya geçilmez.

### Milestone 0 — Kararlar ve çalışma sözleşmesi

**Çıktılar**

- Kritik karar kapılarının onayı
- `AGENTS.md`, başlangıç ADR'leri ve doküman iskeleti
- Branch/CI stratejisi, code style ve Definition of Done

**Doğrulama**

- Repo yapısı ve komutların temiz ortamda uygulanabilirlik kontrolü

### Milestone 1 — Temel iskelet

**İşler**

- .NET solution ve dört backend proje; Angular workspace
- Merkezi package/version ve analyzer ayarları
- Minimal API, OpenAPI, Problem Details, Serilog ve OpenTelemetry temeli
- SQL Server, API ve frontend içeren Docker Compose
- Environment/options validation, `.env.example`, health endpointleri
- Unit/integration/architecture/e2e test projeleri ve temel smoke test
- CI: restore, format/analyzer, build, unit test ve frontend test

**Kabul kriterleri**

- Backend ve frontend temiz checkout'ta derlenir.
- `docker compose up` ile üç servis sağlıklı başlar.
- `/health/live`, `/health/ready` ve OpenAPI erişilebilir.

### Milestone 2 — Film kataloğu

**İşler**

- Movie, Genre, Person/cast ve external reference modeli
- EF Core DbContext, ilk migration, indeksler ve seed altyapısı
- Mock metadata provider ve opsiyonel TMDb adapter iskeleti
- İdempotent now-playing/upcoming sync ve loglama
- Film listeleme, arama, filtre, sayfalama ve detay endpointleri
- Angular ana sayfa, film listesi ve film detay ekranı

**Kabul kriterleri**

- En az 20 seed film mock modda görülebilir.
- Arama/filtre/pagination integration testleri SQL Server container üzerinde geçer.
- Sağlayıcı anahtarı olmadan startup ve sync çalışır.

### Milestone 3 — Sinemalar ve seanslar

**İşler**

- City, District, Cinema, Auditorium ve Showtime modeli
- Mock seans provider, 7 günlük seed ve idempotent sync
- Yakın sinema/Haversine, film ve sinema bazlı seans filtreleri
- Saat dilimi, bitiş zamanı, fiyat, dil, format ve iptal kuralları
- Admin CRUD ve manuel sync endpoint/ekranları
- Sinema liste/detay ve seans UI'ı; konum ret fallback'i

**Kabul kriterleri**

- 3 şehir × 3 sinema × 3 salon ve çeşitli seanslar erişilebilir.
- UTC/İstanbul dönüşümü ve Haversine unit testleri geçer.
- Admin olmayan kullanıcı yönetim işlemi yapamaz.

### Milestone 4 — Öneri motoru

**İşler**

- `RecommendationFilter` şeması, FluentValidation ve tarih/dil/tür normalizasyonu
- Mock parser/ranker ve gerçek AI adapter portu
- Kesin aday sorgusu, 20 aday sınırı ve aday snapshot modeli
- Yapılandırılmış AI çıktı doğrulaması ve aday kimliği güvenlik kontrolü
- Yapılandırılabilir deterministik fallback skorlayıcı
- RecommendationSession/Result persistence ve en fazla üç sonuç
- Ana doğal dil girişi ve öneri sonuç kartları
- Timeout, geçersiz JSON, hallucinated id ve fallback testleri

**Kabul kriterleri**

- Verilen örnek istek beklenen filtrelere dönüşür.
- Zorunlu koşulu ihlal eden aday sonuçlara giremez.
- AI kapalıyken aynı girdiden deterministik sonuç çıkar.
- Sonuçlarda yalnızca aday kümesindeki film-seans çiftleri bulunur.

### Milestone 5 — Kullanıcı özellikleri

**İşler**

- Identity, rol/policy ve güvenli auth akışı
- Tercihler, favoriler, izlenenler ve kullanıcı puanları
- Profil ve öneri geçmişi ekranları
- Öneri skorunda kullanıcı geçmişi girdisi
- Hesap ve kullanıcı verisi silme akışı

**Kabul kriterleri**

- Kullanıcı yalnızca kendi verisini görür/değiştirir.
- Favori/izlenen/puan endpointleri idempotency ve conflict kurallarını uygular.
- AuthN/AuthZ integration testleri geçer.

### Milestone 6 — PWA, kalite ve yayın hazırlığı

**İşler**

- PWA manifest/service worker ve güvenli cache stratejisi
- Mobil responsive, WCAG AA, klavye ve screen-reader düzeltmeleri
- Playwright ana akışı ve konum reddi senaryosu
- Sorgu planı/indeks, N+1 ve payload performans incelemesi
- Rate limit, URL allowlist, secret/PII logging ve dependency güvenlik kontrolü
- README ve tüm operasyon/mimari dokümanlarının tamamlanması
- Docker Compose temiz ortam provası

**Kabul kriterleri**

- Tüm backend/frontend build ve testleri uyarısız geçer.
- Playwright kritik akışları geçer.
- API anahtarsız mock mod ve Docker Compose çalışır.
- Kritik/yüksek güvenlik bulgusu kalmaz veya kabul edilmiş risk olarak belgelenir.

### Milestone 7 — MVP sonrası genişleme

- Grup önerisi ve adil uzlaşma/ceza algoritması
- Lisanslı gerçek seans ve bilet sağlayıcısı
- Harita/rota servisi
- Gerektiğinde Redis/distributed cache
- Üretim SLO, dashboard, alarm ve kapasite testleri

## 14. Test stratejisi

### Unit

- Filtreler, bitiş zamanı, tür dahil/hariç, Haversine, skor normalizasyonu/tie-break, AI çıktı doğrulama ve Result→HTTP eşleme
- Grup algoritmasının domain temeli MVP'de test edilebilir; kullanıcı akışı Milestone 7'ye kalır

### Integration

- Testcontainers ile gerçek SQL Server ve migration
- Minimal API endpointleri, validation, Problem Details ve auth policy
- WireMock.Net ile dış provider başarı, timeout, retry ve bozuk cevapları
- Sync idempotency ve unique constraint davranışı
- AI fallback ve candidate-id doğrulaması

### Frontend

- Vitest ile component/service/state testleri
- Playwright ile ana kullanıcı akışı, konum reddi ve temel admin akışı
- Accessibility için otomatik kontrol; kritik ekranlarda manuel klavye/screen-reader gözden geçirmesi

### Kalite kapısı

Her milestone için:

```text
dotnet restore
dotnet build --no-restore
dotnet test --no-build
npm ci
npm run lint
npm run test -- --run
npm run build
```

Playwright ve Docker Compose smoke testleri uygun milestone'dan itibaren CI veya ayrı pipeline'da çalıştırılır. Gerçek komutlar scaffold sonrasında `AGENTS.md` ve README'de kesinleştirilir.

## 15. Dokümantasyon teslimleri

- `README.md`: kurulum, env, migration, seed, test, Docker, TMDb/AI ve mock mod
- `AGENTS.md`: repo kuralları, komutlar, endpoint/test ve AI güvenliği
- `docs/ARCHITECTURE.md`: sınırlar, bağımlılıklar ve veri akışı
- `docs/API.md`: endpoint ve hata/pagination sözleşmeleri
- `docs/DATA-PROVIDERS.md`: provider mapping, lisans, sync ve fallback
- `docs/AI-RECOMMENDATION.md`: şema, prompt sınırı, doğrulama, scoring ve fallback
- `docs/SECURITY.md`: threat model, auth, secret, PII, URL ve rate-limit politikası
- `docs/DECISIONS/`: modular monolith, auth, scheduler, AI adapter, persistence ve cache ADR'leri

## 16. Riskler ve azaltma yaklaşımı

| Risk | Azaltma |
|---|---|
| Türkiye için güvenilir seans verisi bulunmaması | Mock + manuel yönetim; provider portu; lisanslı sağlayıcı kararı MVP sonrası |
| AI'ın yanlış filtre/kimlik üretmesi | JSON schema + FluentValidation + aday allowlist + deterministik fallback |
| Tarih/saat belirsizliği | Clock abstraction, açık timezone, UTC persistence ve sınır testleri |
| TMDb atıf/lisans ihlali | Provider dokümantasyonu, UI attribution ve varlıkları repoya kopyalamama |
| E2E ortamının ağır olması | Unit ağırlıklı piramit; seçili SQL integration ve kritik Playwright senaryoları |
| Kişisel veri/log sızıntısı | Veri minimizasyonu, redaction, retention ve telemetry denylist |
| Greenfield kapsamının büyümesi | Milestone kabul kriterleri; grup ve gerçek bilet entegrasyonunu MVP dışında tutma |

## 17. Uygulama sırası ve onay noktası

Kodlama, bu plan onaylandıktan sonra Milestone 0 ve Milestone 1 ile başlamalıdır. Her milestone küçük PR/commit dilimlerine ayrılmalı; her dilimde çalışan build, ilgili testler, değişiklik özeti ve sonraki adım raporlanmalıdır. Milestone 1 sırasında teknoloji sürümlerinin NuGet/npm üzerindeki kararlı ve birbiriyle uyumlu sürümleri resmi kaynaklardan doğrulanmalıdır.
