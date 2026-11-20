# Nalazi inspekcije koda – SakuraWeb (ooad-2025-26/Grupa1-Tim4)

Putanje su relativne u odnosu na `Implementacija/SakuraWeb/`. Brojevi linija odgovaraju commitu `fa92eda`.

Tip greške je po dokumentu „Greške opisi“: 1 kritična, 2 ozbiljna, 3 umjerena, 4 mala, 5 kozmetička.

Tekst u okviru ispod svakog nalaza je komentar u formatu iz uputa (opis, greška, checklista), objavljen u pull requestu.

Oznake N01–N62 su iz radne liste pripreme; ovdje su samo nalazi koje je tim potvrdio i objavio.

CWE i OWASP oznake prije objave provjeriti na cwe.mitre.org i owasp.org/Top10.


## Amna (5 nalaza)

### N01 – `Controllers/RezervacijaController.cs`, linije 339-351 · issue kartica

```
DeleteConfirmed (POST) nema [Authorize] atribut. Neprijavljen korisnik može obrisati bilo koju rezervaciju slanjem POST zahtjeva sa ID-em.
Greška: tip 2.
Checklista: Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju? (Kontrola pristupa, sigurnosna lista)
```
- Kategorija: Kontrola pristupa · CWE-862 / A01:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Dodati [Authorize(Roles = "Administrator, Klijent")] i provjeru da je klijent vlasnik rezervacije.

### N02 – `Controllers/RezervacijaController.cs`, linije 125-144

```
Details: bilo koji Klijent ili Frizer može promjenom ID-a u URL-u vidjeti tuđu rezervaciju (IDOR). Otkazi (linija 69) ima provjeru vlasništva, Details nema.
Greška: tip 2.
Checklista: Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo pristupa tim podacima? (Kontrola pristupa, sigurnosna lista)
```
- Kategorija: Kontrola pristupa · CWE-639 / A01:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Istu provjeru vlasništva kao u Otkazi izdvojiti u privatnu metodu i pozvati u Details.

### N04 – `Controllers/RezervacijaController.cs`, linije 199-204

```
Zauzeti termini se ne filtriraju po frizeru (frizerId). Rezervacija kod jednog frizera blokira isti termin kod svih frizera, iako korisnik bira frizera.
Greška: tip 2.
Checklista: Are all loops, branches, and logic constructs complete, correct, and properly nested?
```
- Kategorija: Petlje i grananja
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: U upit dodati uslov r.frizerId == frizerId (isto u ZauzetiTermini, linije 361-366).

### N06 – `Controllers/RezervacijaController.cs`, linije 285-314

```
Edit (POST) pomjera rezervaciju bez ikakve provjere: termin može biti zauzet, van radnog vremena ili u prošlosti. Provjere iz Create nisu ponovljene.
Greška: tip 2.
Checklista: Are all loops, branches, and logic constructs complete, correct, and properly nested?
```
- Kategorija: Petlje i grananja
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: Provjeru zauzetosti izdvojiti u zajedničku metodu i koristiti je i u Create i u Edit.

### N08 – `Controllers/RezervacijaController.cs`, linije 179-212, 371-374

```
Magični brojevi: 30 (trajanje slota) se ponavlja na 6 mjesta, a 16:30 (kraj radnog vremena) je upisano direktno u kod.
Greška: tip 4.
Checklista: Are symbolics used rather than "magic number" constants or string constants?
```
- Kategorija: Struktura
- Uzrok: Nesaglasnost sa standardima kodiranja i dokumentacije
- Prijedlog ispravke: Uvesti konstante TrajanjeSlotaMin i KrajRadnogVremena.


## Amina (5 nalaza)

### N16 – `Controllers/ProizvodiController.cs`, linije 14-308

```
Cijeli kontroler nema [Authorize]. Neprijavljen korisnik može kreirati, mijenjati i brisati proizvode (Create, Edit, Delete).
Greška: tip 2.
Checklista: Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju? (Kontrola pristupa, sigurnosna lista)
```
- Kategorija: Kontrola pristupa · CWE-862 / A01:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Dodati [Authorize(Roles = "Administrator")] na Create, Edit i Delete akcije.

### N19 – `Controllers/ProizvodiController.cs`, linije 30

```
Uslov kategorija.Equals("Sve") poredi enum sa stringom i uvijek je false, pa je drugi dio uslova suvišan i zbunjuje.
Greška: tip 3.
Checklista: Are all loops, branches, and logic constructs complete, correct, and properly nested?
```
- Kategorija: Petlje i grananja
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Ostaviti samo kategorija.HasValue.

### N24 – `Controllers/ProizvodiController.cs`, linije 67, 71, 83-90

```
Zakomentarisan kod ostavljen u produkcijskoj verziji (i UslugaController linija 16).
Greška: tip 4.
Checklista: Are there any leftover stubs or test routines in the code?
```
- Kategorija: Dokumentacija
- Uzrok: Nedostaci procesa testiranja i pregleda
- Prijedlog ispravke: Obrisati zakomentarisane linije.

### N25 – `Controllers/ProizvodiController.cs`, linije 7 (i using direktive u ostalim kontrolerima)

```
Nekorištene using direktive, npr. Microsoft.CodeAnalysis.Scripting.Hosting, System.Net.Mail u NewsletterController (14-15), Microsoft.IdentityModel.Tokens u RacunController (6).
Greška: tip 4.
Checklista: Are there any uncalled or unneeded procedures or any unreachable code?
```
- Kategorija: Struktura
- Uzrok: Nesaglasnost sa standardima kodiranja i dokumentacije
- Prijedlog ispravke: Ukloniti nekorištene using direktive (IDE0005).

### N26 – `Models/Proizvod.cs`, linije 19 (Models/Usluga.cs 15)

```
Cijena se čuva kao double. Tip sa pokretnim zarezom nije pogodan za novčane iznose zbog grešaka zaokruživanja.
Greška: tip 3.
Checklista: Does the code systematically prevent rounding errors?
```
- Kategorija: Aritmetičke operacije
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Koristiti decimal za cijene.


## Dževad (5 nalaza)

### N27 – `Controllers/RacunController.cs`, linije 164-167

```
Nova lozinka se korisniku šalje e-mailom u čistom tekstu.
Greška: tip 2.
Checklista: Da li su osjetljivi podaci zaštićeni pri prenosu i čuvanju? (Kriptografija i zaštita podataka, sigurnosna lista)
```
- Kategorija: Kriptografija i zaštita podataka · CWE-319 / A04:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Poslati link za reset lozinke sa tokenom umjesto same lozinke.

### N29 – `Controllers/RacunController.cs`, linije 70-91 (Models/Korisnik.cs 10)

```
Polje lozinka se veže direktno iz forme i CreateAsync ga upisuje u bazu u čistom tekstu. Tek naknadnim UpdateAsync se zamjenjuje kopijom hash-a. Ako drugi korak ne uspije, lozinka ostaje čitljiva u bazi. Polje je uz to duplikat PasswordHash.
Greška: tip 2.
Checklista: Da li se lozinke čuvaju isključivo u heširanom obliku? (Kriptografija i zaštita podataka, sigurnosna lista)
```
- Kategorija: Kriptografija i zaštita podataka · CWE-256 / A04:2025
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: Ukloniti polje lozinka iz modela; koristiti poseban ViewModel za formu.

### N30 – `Controllers/RacunController.cs`, linije 240-261

```
Brisanje korisnika koji ima rezervacije baca DbUpdateException (veza je Restrict) koji nije obrađen, pa korisnik vidi grešku 500. Administrator može obrisati i vlastiti račun.
Greška: tip 2.
Checklista: Da li aplikacija ispravno obrađuje izuzetke i prikazuje generičku poruku? (Upravljanje greškama i logiranje, sigurnosna lista)
```
- Kategorija: Upravljanje greškama i logiranje · A10:2025
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Obraditi izuzetak i zabraniti brisanje vlastitog računa.

### N32 – `Controllers/RacunController.cs`, linije 75-76 (Models/Korisnik.cs 8-9)

```
korisnickoIme i emailAdresa su duplikati polja UserName i Email iz IdentityUser i ručno se usklađuju. Kada korisnik promijeni e-mail kroz Identity stranice, emailAdresa se ne ažurira, pa obavijesti idu na staru adresu.
Greška: tip 2.
Checklista: Does the code completely and correctly implement the design?
```
- Kategorija: Struktura
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: Koristiti samo UserName i Email iz IdentityUser.

### N34 – `Controllers/RacunController.cs`, linije 182

```
Varijabla options se dodjeljuje, a nikad ne koristi.
Greška: tip 4.
Checklista: Are there any redundant or unused variables?
```
- Kategorija: Varijable
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Obrisati liniju.


## Hana (5 nalaza)

### N37 – `Controllers/AnketaController.cs`, linije 13-218 · issue kartica

```
Cijeli kontroler nema [Authorize]. Neprijavljen korisnik može kreirati, mijenjati i brisati ankete.
Greška: tip 2.
Checklista: Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju? (Kontrola pristupa, sigurnosna lista)
```
- Kategorija: Kontrola pristupa · CWE-862 / A01:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Dodati [Authorize(Roles = "Administrator")] na Create, Edit i Delete.

### N40 – `Controllers/AnketaController.cs`, linije 181-183

```
Atribut [HttpPost] i komentar su napisani dva puta zaredom.
Greška: tip 4.
Checklista: Are all comments consistent with the code?
```
- Kategorija: Dokumentacija
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Obrisati duplikat.

### N44 – `Controllers/NewsletterController.cs`, linije 99

```
Console.WriteLine ispisuje e-mail adresu i lozinku pošiljaoca u izlaz aplikacije (log).
Greška: tip 2.
Checklista: Da li su povjerljivi podaci maskirani u logovima? (Upravljanje greškama i logiranje, sigurnosna lista)
```
- Kategorija: Upravljanje greškama i logiranje · CWE-532 / A09:2025
- Uzrok: Nedostaci procesa testiranja i pregleda
- Prijedlog ispravke: Obrisati liniju.

### N45 – `Models/Newsletter.cs`, linije 10 (NewsletterController 146)

```
Lozinka e-mail naloga (lozinkaZaEmail) se čuva u bazi u čistom tekstu i prikazuje i mijenja kroz Edit formu.
Greška: tip 2.
Checklista: Da li su tajni podaci izmješteni u konfiguraciju ili varijable okruženja? (Upravljanje tajnama, sigurnosna lista)
```
- Kategorija: Upravljanje tajnama · CWE-256 / A04:2025
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: Čuvati lozinku u User Secrets ili varijabli okruženja.

### N46 – `Controllers/NewsletterController.cs`, linije 69-79

```
Create ne provjerava ModelState.IsValid, pa se poruka sprema i šalje i kada naziv ili tekst nisu validni.
Greška: tip 2.
Checklista: Da li se provodi striktna validacija svih podataka sa klijentske strane? (Validacija unosa, sigurnosna lista)
```
- Kategorija: Validacija unosa · CWE-20
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Dodati if (!ModelState.IsValid) return View(poruka);


## Ilma (6 nalaza)

### N50 – `Controllers/UslugaController.cs`, linije 162-172

```
Brisanje usluge kaskadno briše sve njene rezervacije (uslugaId je Cascade), bez upozorenja i bez obavijesti klijentima.
Greška: tip 2.
Checklista: Are the correct data operated on in each statement?
```
- Kategorija: Defanzivno programiranje
- Uzrok: Greška u logičkom dizajnu
- Prijedlog ispravke: Zabraniti brisanje usluge koja ima aktivne rezervacije.

### N51 – `Controllers/UslugaController.cs`, linije 36-38

```
Javna akcija Index ([AllowAnonymous]) u ViewBag stavlja kompletne Korisnik objekte frizera, uključujući PasswordHash, lozinka i e-mail.
Greška: tip 3.
Checklista: Da li su osjetljivi podaci zaštićeni? (Kriptografija i zaštita podataka, sigurnosna lista)
```
- Kategorija: Kriptografija i zaštita podataka · CWE-200
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Sa Select izdvojiti samo Id i korisničko ime.

### N52 – `Models/Usluga.cs`, linije 11-12 (Rezervacija.cs 37, 41; ProizvodBenefit.cs 25-26)

```
Uz uključen Nullable, svojstva koja ne smiju biti null nisu inicijalizirana ili im se eksplicitno dodjeljuje null.
Greška: tip 3.
Checklista: Do all assigned variables have proper type consistency or casting?
```
- Kategorija: Varijable
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Inicijalizirati svojstva ili ih označiti kao nullable (?).

### N56 – `Services/EmailSender.cs`, linije 53-66

```
SMTP server i port su upisani direktno u kod. MailMessage implementira IDisposable, a ne oslobađa se.
Greška: tip 3.
Checklista: Are all files and devices left in the correct state upon program termination?
```
- Kategorija: Defanzivno programiranje
- Uzrok: Greška kodiranja
- Prijedlog ispravke: Dodati using za MailMessage; server i port premjestiti u konfiguraciju.

### N58 – `Data/ApplicationUser.cs`, linije 1-6

```
Klasa ApplicationUser se nigdje ne koristi (aplikacija koristi Korisnik). U .csproj je ostala i stavka Compile Remove za nepostojeći fajl.
Greška: tip 4.
Checklista: Are there any uncalled or unneeded procedures or any unreachable code?
```
- Kategorija: Struktura
- Uzrok: Nedostaci procesa testiranja i pregleda
- Prijedlog ispravke: Obrisati klasu i stavku iz .csproj.

### N61 – `appsettings.json`, linije 3

```
U repozitoriju je konekcijski string produkcijske baze (server, naziv baze, administratorski korisnik). Lozinka je prazna, ali ostali podaci su otkriveni. TrustServerCertificate=True isključuje provjeru certifikata servera.
Greška: tip 3.
Checklista: Ima li u kodu konekcijskih stringova ili drugih tajni? (Upravljanje tajnama, sigurnosna lista)
```
- Kategorija: Upravljanje tajnama · CWE-295 / A02:2025
- Uzrok: Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)
- Prijedlog ispravke: Konekcijski string držati u User Secrets ili varijabli okruženja; ukloniti TrustServerCertificate.
