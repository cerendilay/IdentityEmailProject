# Mailora | Identity E-Posta Yönetim Sistemi

ASP.NET Core MVC (.NET 10) + Identity + EF Core + SQL Server ile geliştirilmiş, kullanıcıya özel çalışan e-posta yönetim sistemi.

## Kurulum

1. **Veritabanı bağlantısı:** `MYAcademy_IdentityEmailProject/Context/EmailContext.cs` içindeki connection string'i kendi SQL Server'ınıza göre düzenleyin
   (varsayılan: `Server=localhost; Database=IdentityEmailDb; Integrated Security`).
2. **Migration'ları uygulayın** (Visual Studio → Package Manager Console):
   ```
   Update-Database
   ```
   veya terminalden: `dotnet ef database update`
3. **Kategorileri ekleyin:** Kategoriler veritabanından gelir. `Categories` tablosuna en az bir kayıt ekleyin
   (ör. İş ve Kariyer, Kişisel, Finans...).
4. **SMTP ayarları:** `MYAcademy_IdentityEmailProject/appsettings.json` → `MailSettings` bölümüne Gmail adresinizi yazın.
   Gmail **uygulama şifresi** güvenlik için repoda tutulmaz; proje klasöründe şu komutla User Secrets'a ekleyin
   (Visual Studio'da: proje → sağ tık → *Manage User Secrets*):
   ```
   dotnet user-secrets set "MailSettings:Password" "uygulama-sifreniz"
   ```
5. Projeyi çalıştırın (`http://localhost:5101`).

> Not: Giriş için e-posta doğrulaması zorunludur. Bu özellik eklenmeden önce kayıt olmuş kullanıcılar
> giriş ekranındaki **"E-postamı Doğrula"** sayfasından "Kodu tekrar gönder" diyerek yeni kod alabilir.

## Migration'lar

| Migration | İçerik |
|---|---|
| `InitialCreate` | Identity tabloları, Categories, Messages |
| `mig2`, `AddValidCode` | AppUser ek alanları, 6 haneli doğrulama kodu |
| `mig_DraftTrashCategoryNullable` | Messages: `IsDraft`, `IsDeleted` alanları; `CategoryId` taslaklar için boş olabilir |

## Özellikler (Case maddeleri)

- **Üyelik:** Register / Login / Logout (Identity). Kayıtta 6 haneli kod e-posta ile gönderilir; kod doğrulanmadan giriş yapılamaz.
- **Hata sayfaları:** 401, 403 (başkasının mesajına erişim), 404 (olmayan sayfa/mesaj) → `ErrorPages`.
- **Şifre:** "Şifremi Unuttum" → e-posta ile tek kullanımlık, 1 saat geçerli yenileme bağlantısı. Geçersiz/süresi dolmuş bağlantı uyarısı. Profil ekranından şifre değiştirme.
- **Güvenlik:** Tüm mesaj sorguları oturum açan kullanıcının e-postasına göre filtrelenir; URL'deki id değiştirilerek başkasının mesajı görüntülenemez/düzenlenemez. Mesaj içeriği sandbox iframe içinde gösterilir (XSS koruması).
- **Gelen Kutusu:** Sadece alıcıya ait mesajlar; arama, kategori filtresi, tarihe göre sıralama, okundu/okunmadı, yıldızlama, toplu işlemler.
- **Yeni Mesaj:** Gönderen Identity hesabından alınır; alıcı/kategori/konu/içerik doğrulanır. Kayıtlı kullanıcıya gönderilen mesaj alıcının Gelen Kutusuna ve gönderenin Gönderilenler'ine düşer; kayıtlı olmayan adrese SMTP ile gerçek e-posta gönderilir.
- **Gönderilenler, Yıldızlı, Taslaklar, Çöp Kutusu:** Taslak kaydetme/düzenleme/gönderme; çöp kutusuna taşıma, geri alma, kalıcı silme; yanıtla ve ilet.
- **Profil:** Ad, soyad, açıklama ve profil fotoğrafı (`wwwroot/images/profile`) güncelleme. Navbar'da kullanıcı bilgileri ve okunmamış mesaj bildirimleri.
- **Dashboard:** Gelen, gönderilen, okunmamış, yıldızlı ve taslak sayıları; son mesajlar; kategorilere göre dağılım grafiği (Chart.js).
