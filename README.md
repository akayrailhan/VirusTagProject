Virus Tag: Protocol Shift 🦠

CENG462 Multiplayer Game Jam Projesi

"Avcı mısın yoksa av mı? Kimliğin her an değişebilir!"

📖 Proje Hakkında

Virus Tag, Unity Netcode for GameObjects (NGO) ve Unity Gaming Services (UGS) kullanılarak geliştirilmiş, 2D Top-Down (Kuşbakışı) bir multiplayer oyundur.

Tema: "Identity Shift" (Kimlik Değişimi).
Konsept: Klasik ebelemece (tag) oyununun, dijital bir virüs temasıyla harmanlanmış hali. Oyuncular "Virüslü" veya "Temiz" olarak başlar. Virüslü oyuncu, hayatta kalmak için virüsü mermi (projectile) yoluyla başkasına bulaştırmalıdır. Vurulan oyuncu anında virüslüye dönüşür ve roller değişir.

✨ Özellikler (Tamamlanan Görevler)

Bu proje, CENG462 dersinin zorunlu teknik görevlerini (Questline) kapsar:

🌐 Networking & Altyapı

Callsign Forge: Oyuncular giriş ekranında isimlerini belirler ve kaydeder.

Warp Gate Handshake: Unity Relay servisi üzerinden güvenli (DTLS) bağlantı.

Lobby Observatory: Oyuncular açık odaları (Lobbyleri) listeleyebilir ve tek tıkla katılabilir.

Host Beacon: Host, odayı kurduğunda Relay kodu otomatik olarak lobiye gömülür.

Authentication: Unity Authentication servisi ile anonim giriş (Anonymous Login).

🎮 Oynanış & Senkronizasyon

Client-Authoritative Movement: Gecikmesiz (Responsive) hareket sistemi.

Lag Compensation (Basit): Mermi atışlarında "Dummy Projectile" kullanılarak anında görsel tepki.

Lobby Heartbeat: Host oyuna başlasa bile lobi listesinde görünür kalmasını sağlayan arka plan servisi.

🚀 Kurulum ve Çalıştırma

Projeyi kendi bilgisayarınızda çalıştırmak için şu adımları izleyin:

Gereksinimler

Unity 6 (6000.x) veya üzeri.

Unity Hub.

İnternet bağlantısı (Lobby ve Relay servisleri için).

Adım Adım

Repoyu Klonlayın:

git clone https://github.com/KULLANICI_ADINIZ/CENG462-VirusTag.git


Unity ile Açın:
Unity Hub'ı açın, "Add" diyerek klasörü seçin ve projeyi başlatın.

Bootstrap Sahnesi:
Scenes/Bootstrap sahnesini açın. Oyun her zaman bu sahneden başlamalıdır!

Test Etme (Host & Client):

Host: Unity Editöründe "Play" tuşuna basın. İsim girin -> Connect -> Create Lobby.

Client: File > Build Settings menüsünden bir "Build" alın. Çalıştırın, farklı bir isim girin -> Connect -> Refresh -> Join.

🕹️ Nasıl Oynanır?

Giriş: Adınızı girin ve "Connect" butonuna basın.

Lobi:

Oda kurmak için bir isim yazıp "Create Lobby" deyin.

Mevcut bir odaya girmek için "Refresh" yapın ve listeden "Join" butonuna basın.

Oyun (Yakında):

WASD: Hareket et.

Mouse: Nişan al.

Sol Tık: Ateş et (Virüsü bulaştır!).

🛠️ Teknoloji Yığını

Oyun Motoru: Unity 2022.3 / Unity 6 (URP)

Dil: C#

Networking: Unity Netcode for GameObjects (NGO) 1.x

Servisler (UGS):

Authentication

Lobby

Relay

Veri Yönetimi: PlayerPrefs (Yerel), NetworkVariables (Senkronizasyon)

📅 Geliştirme Günlüğü

Day 1: Proje kurulumu, UGS paketleri, Bootstrap sahnesi, Auth sistemi ve ApplicationController mimarisi.

Day 2: Lobby UI tasarımı, Relay entegrasyonu, Lobi kurma/listeleme/katılma sistemi, Heartbeat mekanizması.

Day 3: (Planlanan) Karakter hareketi, nişan alma, virüs mekaniği ve leaderboard.
