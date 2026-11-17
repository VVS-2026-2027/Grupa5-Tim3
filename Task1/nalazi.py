"""Generise tabelu nalaza, CSV check sheet i Pareto dijagrame za Task 1."""
import csv, sys, os
from collections import Counter
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)

# Kategorije defekata = kategorije lista provjere (P2 Wiegers, P3 sigurnosna)
STR, DOK, VAR, ARI, PET, DEF = ("Struktura", "Dokumentacija", "Varijable",
                                "Aritmetičke operacije", "Petlje i grananja",
                                "Defanzivno programiranje")
VAL, TAJ, PRI, KRI, GRE, ZAV = ("Validacija unosa", "Upravljanje tajnama",
                                "Kontrola pristupa", "Kriptografija i zaštita podataka",
                                "Upravljanje greškama i logiranje",
                                "Sigurnost vanjskih komponenti")
SIG = {VAL, TAJ, PRI, KRI, GRE, ZAV}

# Uzroci (Predavanje 1, poglavlje 1.4)
U_ZAH = "Greška u definiciji zahtjeva (nedostaju sigurnosni zahtjevi)"
U_DIZ = "Greška u logičkom dizajnu"
U_KOD = "Greška kodiranja"
U_STD = "Nesaglasnost sa standardima kodiranja i dokumentacije"
U_TST = "Nedostaci procesa testiranja i pregleda"

RC = "Controllers/RezervacijaController.cs"
PC = "Controllers/ProizvodiController.cs"
AC = "Controllers/RacunController.cs"
NC = "Controllers/AnketaController.cs"
LC = "Controllers/NewsletterController.cs"
UC = "Controllers/UslugaController.cs"
HC = "Controllers/HomeController.cs"
ES = "Services/EmailSender.cs"
DB = "Data/ApplicationDbContext.cs"

AMNA, AMINA, DZEVAD, ILMA, HANA = "Amna", "Amina", "Dževad", "Ilma", "Hana"

# (fajl, linije, opis, kategorija, stavka liste, ozbiljnost, uzrok, CWE/OWASP, inspektor, prijedlog)
N = [
 # ---------------- RezervacijaController (Amna)
 (RC, "339-351", "DeleteConfirmed (POST) nema [Authorize] atribut. Neprijavljen korisnik može obrisati bilo koju rezervaciju slanjem POST zahtjeva sa ID-em.",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju?", 5, U_ZAH, "CWE-862 / A01:2025", AMNA,
  "Dodati [Authorize(Roles = \"Administrator, Klijent\")] i provjeru da je klijent vlasnik rezervacije."),
 (RC, "125-144", "Details: bilo koji Klijent ili Frizer može promjenom ID-a u URL-u vidjeti tuđu rezervaciju (IDOR). Otkazi (linija 69) ima provjeru vlasništva, Details nema.",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo pristupa tim podacima?", 4, U_ZAH, "CWE-639 / A01:2025", AMNA,
  "Istu provjeru vlasništva kao u Otkazi izdvojiti u privatnu metodu i pozvati u Details."),
 (RC, "317-336", "Delete (GET): Klijent može otvoriti stranicu za brisanje tuđe rezervacije, bez provjere vlasništva.",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo pristupa tim podacima?", 4, U_ZAH, "CWE-639 / A01:2025", AMNA,
  "Dodati provjeru klijentId == prijavljeni korisnik (osim za administratora)."),
 (RC, "199-204", "Zauzeti termini se ne filtriraju po frizeru (frizerId). Rezervacija kod jednog frizera blokira isti termin kod svih frizera, iako korisnik bira frizera.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 3, U_DIZ, "", AMNA,
  "U upit dodati uslov r.frizerId == frizerId (isto u ZauzetiTermini, linije 361-366)."),
 (RC, "163-246", "Create (POST) ne provjerava da li je datum u prošlosti, da li je početno vrijeme prije početka radnog vremena i da li je na granici od 30 minuta. Moguće je rezervisati termin u prošlosti ili u 03:17.",
  VAL, "Da li se provodi striktna validacija svih podataka sa klijentske strane?", 3, U_DIZ, "CWE-20", AMNA,
  "Dodati provjere datuma i vremena prije kreiranja rezervacije."),
 (RC, "285-314", "Edit (POST) pomjera rezervaciju bez ikakve provjere: termin može biti zauzet, van radnog vremena ili u prošlosti. Provjere iz Create nisu ponovljene.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 3, U_DIZ, "", AMNA,
  "Provjeru zauzetosti izdvojiti u zajedničku metodu i koristiti je i u Create i u Edit."),
 (RC, "199-237", "Provjera zauzetosti i upis rezervacije nisu atomska operacija (nema transakcije ni jedinstvenog indeksa). Dva istovremena zahtjeva mogu rezervisati isti termin.",
  DEF, "Are the correct data operated on in each statement?", 3, U_DIZ, "CWE-362", AMNA,
  "Koristiti transakciju ili jedinstveni indeks nad (frizerId, vrijemeTermina)."),
 (RC, "179-212, 371-374", "Magični brojevi: 30 (trajanje slota) se ponavlja na 6 mjesta, a 16:30 (kraj radnog vremena) je upisano direktno u kod.",
  STR, "Are symbolics used rather than \"magic number\" constants or string constants?", 2, U_STD, "", AMNA,
  "Uvesti konstante TrajanjeSlotaMin i KrajRadnogVremena."),
 (RC, "148-157, 248-258, 206-214, 368-377", "Ponovljeni kod: Create (GET) i VratiCreateView su identični; računanje zauzetih slotova je kopirano u Create i ZauzetiTermini; isti lanac Include se ponavlja 6 puta.",
  STR, "Are there any blocks of repeated code that could be condensed into a single procedure?", 2, U_KOD, "", AMNA,
  "Izdvojiti privatne metode (npr. DajZauzeteSlotove, DajRezervacijuSaVezama)."),
 (RC, "56-95, 101-122", "Otkazi dozvoljava otkazivanje već otkazane ili prošle rezervacije (ponovo se šalju e-mailovi). Ocijeni dozvoljava ocjenu otkazane ili buduće rezervacije.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 2, U_DIZ, "", AMNA,
  "Dodati provjere stanja (otkazana, vrijemeTermina < sada)."),
 (RC, "115-121", "Ocijeni: ocjena van opsega 1-5 se tiho ignoriše; korisnik ne dobija nikakvu poruku o grešci.",
  DEF, "Are imported data and input arguments tested for validity and completeness?", 2, U_KOD, "", AMNA,
  "Vratiti poruku o grešci (TempData ili BadRequest)."),
 (RC, "228, 234 (Models/Rezervacija.cs 15, 32)", "Model propisuje [Range(1,5)] za ocjenu, a Create postavlja ocjena = 0. Konstruktor postavlja id = -1, pa je u kontroleru potreban zaobilazni kod IsTemporary = true.",
  VAR, "Do all assigned variables have proper type consistency or casting?", 2, U_KOD, "", AMNA,
  "ocjena promijeniti u int? (null = nije ocijenjeno), ukloniti id = -1 iz konstruktora."),
 (RC, "239-243, 81-91", "Slanje e-maila nakon SaveChanges nije u try/catch. Ako e-mail nije konfigurisan (EmailSender baca Exception), rezervacija je već sačuvana, a korisnik vidi grešku 500. Operator ! na liniji 241 skriva moguću null referencu.",
  GRE, "Da li aplikacija ispravno obrađuje izuzetke i prikazuje generičku poruku?", 3, U_KOD, "CWE-476 / A10:2025", AMNA,
  "Slanje e-maila obuhvatiti try/catch blokom i grešku zapisati u log."),
 (RC, "382-385", "Privatna metoda RezervacijaExists se nigdje ne poziva (mrtav kod). Isto važi za KorisnikExists u RacunController (263-266).",
  STR, "Are there any uncalled or unneeded procedures or any unreachable code?", 1, U_TST, "", AMNA,
  "Obrisati nekorištene metode."),
 (RC, "354-380", "ZauzetiTermini nema [Authorize], iako stranicu za rezervaciju koristi samo prijavljeni Klijent.",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju?", 2, U_ZAH, "CWE-862 / A01:2025", AMNA,
  "Dodati [Authorize(Roles = \"Klijent\")]."),

 # ---------------- ProizvodiController (Amina)
 (PC, "14-308", "Cijeli kontroler nema [Authorize]. Neprijavljen korisnik može kreirati, mijenjati i brisati proizvode (Create, Edit, Delete).",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju?", 5, U_ZAH, "CWE-862 / A01:2025", AMINA,
  "Dodati [Authorize(Roles = \"Administrator\")] na Create, Edit i Delete akcije."),
 (PC, "109-123, 204-218", "Upload slike bez ikakve validacije: ne provjeravaju se ekstenzija, tip sadržaja ni veličina. Ekstenzija se preuzima iz imena koje šalje korisnik, a fajl se sprema u javni folder wwwroot/img (moguć upload .html ili .svg fajla sa skriptom).",
  VAL, "Da li se provodi striktna validacija svih podataka sa klijentske strane?", 4, U_ZAH, "CWE-434 / A06:2025", AMINA,
  "Dozvoliti samo .jpg/.png/.webp, ograničiti veličinu i provjeriti sadržaj fajla."),
 (PC, "105", "Bind lista u Create sadrži id i slikaPutanja, pa korisnik može sam poslati te vrijednosti (overposting). Isto za id u UslugaController (75) i AnketaController (65).",
  VAL, "Da li se provodi striktna validacija svih podataka sa klijentske strane?", 2, U_KOD, "CWE-915", AMINA,
  "Ukloniti id i slikaPutanja iz Bind liste za Create."),
 (PC, "30", "Uslov kategorija.Equals(\"Sve\") poredi enum sa stringom i uvijek je false, pa je drugi dio uslova suvišan i zbunjuje.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 2, U_KOD, "", AMINA,
  "Ostaviti samo kategorija.HasValue."),
 (PC, "125-141, 221-246", "Create koristi 2, a Edit 5 odvojenih poziva SaveChanges bez transakcije. Ako neki ID benefita ili sastojka ne postoji, baca se izuzetak, a proizvod ostaje bez veza (u Edit su stare veze već obrisane).",
  DEF, "Are the correct data operated on in each statement?", 3, U_DIZ, "", AMINA,
  "Sve izmjene uraditi u jednoj transakciji i provjeriti da ID-evi postoje."),
 (PC, "64-81", "Benefiti se učitavaju dva puta: jednom kroz Include(ProizvodBenefiti).ThenInclude(benefit), a zatim posebnim upitom u ViewData.",
  STR, "Are there any uncalled or unneeded procedures or any unreachable code?", 2, U_KOD, "", AMINA,
  "Ukloniti drugi upit i koristiti već učitanu kolekciju."),
 (PC, "109-123, 204-218", "Kod za spremanje slike je kopiran u Create i Edit (15 identičnih linija).",
  STR, "Are there any blocks of repeated code that could be condensed into a single procedure?", 2, U_KOD, "", AMINA,
  "Izdvojiti privatnu metodu SacuvajSliku(IFormFile)."),
 (PC, "217, 293-298", "Pri izmjeni slike i pri brisanju proizvoda stari fajl ostaje na disku (fajlovi bez vlasnika se gomilaju).",
  DEF, "Are all files and devices left in the correct state upon program termination?", 2, U_KOD, "", AMINA,
  "Obrisati stari fajl nakon uspješne izmjene ili brisanja."),
 (PC, "67, 71, 83-90", "Zakomentarisan kod ostavljen u produkcijskoj verziji (i UslugaController linija 16).",
  DOK, "Are there any leftover stubs or test routines in the code?", 1, U_TST, "", AMINA,
  "Obrisati zakomentarisane linije."),
 (PC, "7 (i using direktive u ostalim kontrolerima)", "Nekorištene using direktive, npr. Microsoft.CodeAnalysis.Scripting.Hosting, System.Net.Mail u NewsletterController (14-15), Microsoft.IdentityModel.Tokens u RacunController (6).",
  STR, "Are there any uncalled or unneeded procedures or any unreachable code?", 1, U_STD, "", AMINA,
  "Ukloniti nekorištene using direktive (IDE0005)."),
 ("Models/Proizvod.cs", "19 (Models/Usluga.cs 15)", "Cijena se čuva kao double. Tip sa pokretnim zarezom nije pogodan za novčane iznose zbog grešaka zaokruživanja.",
  ARI, "Does the code systematically prevent rounding errors?", 2, U_KOD, "", AMINA,
  "Koristiti decimal za cijene."),

 # ---------------- RacunController + Korisnik (Dzevad)
 (AC, "164-167", "Nova lozinka se korisniku šalje e-mailom u čistom tekstu.",
  KRI, "Da li su osjetljivi podaci zaštićeni pri prenosu i čuvanju?", 4, U_ZAH, "CWE-319 / A04:2025", DZEVAD,
  "Poslati link za reset lozinke sa tokenom umjesto same lozinke."),
 (AC, "188-208", "Lozinka se generiše klasom System.Random, koja nije kriptografski siguran generator.",
  KRI, "Da li je izbjegnuta upotreba slabih kriptografskih algoritama?", 3, U_KOD, "CWE-338 / A04:2025", DZEVAD,
  "Koristiti System.Security.Cryptography.RandomNumberGenerator."),
 (AC, "70-91 (Models/Korisnik.cs 10)", "Polje lozinka se veže direktno iz forme i CreateAsync ga upisuje u bazu u čistom tekstu. Tek naknadnim UpdateAsync se zamjenjuje kopijom hash-a. Ako drugi korak ne uspije, lozinka ostaje čitljiva u bazi. Polje je uz to duplikat PasswordHash.",
  KRI, "Da li se lozinke čuvaju isključivo u heširanom obliku?", 4, U_DIZ, "CWE-256 / A04:2025", DZEVAD,
  "Ukloniti polje lozinka iz modela; koristiti poseban ViewModel za formu."),
 (AC, "240-261", "Brisanje korisnika koji ima rezervacije baca DbUpdateException (veza je Restrict) koji nije obrađen, pa korisnik vidi grešku 500. Administrator može obrisati i vlastiti račun.",
  GRE, "Da li aplikacija ispravno obrađuje izuzetke i prikazuje generičku poruku?", 3, U_KOD, "A10:2025", DZEVAD,
  "Obraditi izuzetak i zabraniti brisanje vlastitog računa."),
 (AC, "254 (RezervacijaController 84, 91, 243, 310)", "Tekst koji unosi korisnik (razlog, korisničko ime, naziv usluge) ubacuje se bez kodiranja u tijelo e-maila koje se šalje kao HTML (IsBodyHtml = true).",
  VAL, "Da li se provodi sanitizacija i kodiranje unosa?", 3, U_KOD, "CWE-80 / A05:2025", DZEVAD,
  "Kodirati vrijednosti sa WebUtility.HtmlEncode prije ubacivanja u poruku."),
 (AC, "75-76 (Models/Korisnik.cs 8-9)", "korisnickoIme i emailAdresa su duplikati polja UserName i Email iz IdentityUser i ručno se usklađuju. Kada korisnik promijeni e-mail kroz Identity stranice, emailAdresa se ne ažurira, pa obavijesti idu na staru adresu.",
  STR, "Does the code completely and correctly implement the design?", 3, U_DIZ, "", DZEVAD,
  "Koristiti samo UserName i Email iz IdentityUser."),
 (AC, "97, 174", "Rezultat AddToRoleAsync se ne provjerava. Kada reset lozinke ne uspije, korisnik se samo preusmjeri bez poruke o grešci.",
  DEF, "Are timeouts or error traps used for external device accesses?", 2, U_KOD, "", DZEVAD,
  "Provjeriti result.Succeeded i prikazati grešku."),
 (AC, "182", "Varijabla options se dodjeljuje, a nikad ne koristi.",
  VAR, "Are there any redundant or unused variables?", 1, U_KOD, "", DZEVAD,
  "Obrisati liniju."),
 (AC, "95", "Uslov !string.IsNullOrEmpty(enum.ToString()) je uvijek tačan. Komentar kaže da se dodjeljuje uloga Frizer, a kod dodjeljuje bilo koju ulogu iz forme.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 1, U_KOD, "", DZEVAD,
  "Ukloniti uslov i ispraviti komentar."),
 (AC, "129-130", "Komentar kaže 'POST: Racun/Edit/5', a ispod je akcija ResetPassword. Edit (POST) akcija ne postoji.",
  DOK, "Are all comments consistent with the code?", 1, U_STD, "", DZEVAD,
  "Ispraviti komentar."),

 # ---------------- Anketa + Newsletter (Ilma)
 (NC, "13-218", "Cijeli kontroler nema [Authorize]. Neprijavljen korisnik može kreirati, mijenjati i brisati ankete.",
  PRI, "Provjerava li kod na svakoj osjetljivoj ruti da li korisnik ima pravo na akciju?", 4, U_ZAH, "CWE-862 / A01:2025", ILMA,
  "Dodati [Authorize(Roles = \"Administrator\")] na Create, Edit i Delete."),
 (NC, "56, 73, 90, 126", "Create i Edit pune padajuću listu ID-evima svih korisnika iz baze, a akcije su dostupne bez prijave.",
  PRI, "Da li su podaci dostupni samo ovlaštenim korisnicima?", 3, U_ZAH, "CWE-200 / A01:2025", ILMA,
  "korisnikId postaviti na serveru iz prijavljenog korisnika."),
 (NC, "184-188", "Preporuka: ako je tijelo zahtjeva prazno, odgovorIds je null i Contains baca NullReferenceException. Ne provjerava se ni da je odabran po jedan odgovor za svako pitanje.",
  DEF, "Are imported data and input arguments tested for validity and completeness?", 3, U_KOD, "CWE-476 / A10:2025", ILMA,
  "Na početku provjeriti null i praznu listu i vratiti BadRequest."),
 (NC, "181-183", "Atribut [HttpPost] i komentar su napisani dva puta zaredom.",
  DOK, "Are all comments consistent with the code?", 1, U_KOD, "", ILMA,
  "Obrisati duplikat."),
 (NC, "190", "Svi proizvodi se učitavaju u memoriju, a zatim filtriraju u kodu, umjesto upitom u bazi.",
  STR, "Is storage use efficient?", 1, U_KOD, "", ILMA,
  "Filtrirati i sortirati u upitu."),
 (NC, "208-213", "Nedosljedno uvlačenje bloka (linije počinju na pogrešnoj koloni).",
  DOK, "Is the code well-structured, consistent in style, and consistently formatted?", 1, U_STD, "", ILMA,
  "Formatirati dokument (Ctrl+K, Ctrl+D)."),
 ("Models/Pitanje.cs", "10-13 (Data/ApplicationDbContext.cs 58-63)", "Pitanje ima obavezan strani ključ anketaId, a početni podaci (seed) ga ne postavljaju, pa je 0. Rezultat ankete se nigdje ne sprema (Preporuka ne kreira Anketa objekat). PROVJERITI pokretanjem migracije.",
  STR, "Does the code completely and correctly implement the design?", 3, U_DIZ, "", ILMA,
  "Uskladiti model sa stvarnim tokom ankete."),
 (LC, "99", "Console.WriteLine ispisuje e-mail adresu i lozinku pošiljaoca u izlaz aplikacije (log).",
  GRE, "Da li su povjerljivi podaci maskirani u logovima?", 4, U_TST, "CWE-532 / A09:2025", ILMA,
  "Obrisati liniju."),
 ("Models/Newsletter.cs", "10 (NewsletterController 146)", "Lozinka e-mail naloga (lozinkaZaEmail) se čuva u bazi u čistom tekstu i prikazuje i mijenja kroz Edit formu.",
  TAJ, "Da li su tajni podaci izmješteni u konfiguraciju ili varijable okruženja?", 4, U_DIZ, "CWE-256 / A04:2025", ILMA,
  "Čuvati lozinku u User Secrets ili varijabli okruženja."),
 (LC, "69-79", "Create ne provjerava ModelState.IsValid, pa se poruka sprema i šalje i kada naziv ili tekst nisu validni.",
  VAL, "Da li se provodi striktna validacija svih podataka sa klijentske strane?", 3, U_KOD, "CWE-20", ILMA,
  "Dodati if (!ModelState.IsValid) return View(poruka);"),
 (LC, "71-76", "ID newslettera je upisan direktno u kod (1). Ako zapis ne postoji, greška se samo ispiše na konzolu, a korisnik dobije istu formu bez objašnjenja.",
  STR, "Are symbolics used rather than \"magic number\" constants or string constants?", 2, U_KOD, "", ILMA,
  "Uvesti konstantu i dodati ModelState.AddModelError."),
 (LC, "199-209", "DeleteConfirmed briše zapis Newsletter (postavke naloga). Zbog kaskadnog brisanja nestaju i sve poruke, a Create nakon toga više ne radi jer traži zapis sa ID-em 1. Index prikazuje poruke, a Details, Edit i Delete rade nad postavkama.",
  PET, "Are all loops, branches, and logic constructs complete, correct, and properly nested?", 3, U_DIZ, "", ILMA,
  "Razdvojiti upravljanje porukama od postavki naloga; zabraniti brisanje postavki."),
 (LC, "101-116", "E-mailovi se šalju jedan po jedan unutar HTTP zahtjeva, bez obrade grešaka. Kontroler provjerava konkretan tip (is EmailSender) umjesto da koristi interfejs.",
  STR, "Are any modules excessively complex and should be restructured or split into multiple routines?", 2, U_DIZ, "", ILMA,
  "Proširiti interfejs ili uvesti poseban servis za newsletter."),

 # ---------------- Usluga, Home, Services, Data, konfiguracija (Hana)
 (UC, "162-172", "Brisanje usluge kaskadno briše sve njene rezervacije (uslugaId je Cascade), bez upozorenja i bez obavijesti klijentima.",
  DEF, "Are the correct data operated on in each statement?", 3, U_DIZ, "", HANA,
  "Zabraniti brisanje usluge koja ima aktivne rezervacije."),
 (UC, "36-38", "Javna akcija Index ([AllowAnonymous]) u ViewBag stavlja kompletne Korisnik objekte frizera, uključujući PasswordHash, lozinka i e-mail.",
  KRI, "Da li su osjetljivi podaci zaštićeni?", 2, U_KOD, "CWE-200", HANA,
  "Sa Select izdvojiti samo Id i korisničko ime."),
 ("Models/Usluga.cs", "11-12 (Rezervacija.cs 37, 41; ProizvodBenefit.cs 25-26)", "Uz uključen Nullable, svojstva koja ne smiju biti null nisu inicijalizirana ili im se eksplicitno dodjeljuje null.",
  VAR, "Do all assigned variables have proper type consistency or casting?", 2, U_KOD, "", HANA,
  "Inicijalizirati svojstva ili ih označiti kao nullable (?)."),
 (HC, "22-24", "Tri sinhrona poziva Count() nad bazom u jednom zahtjevu, dok ostali kontroleri koriste asinhrone pozive.",
  STR, "Is the code well-structured, consistent in style, and consistently formatted?", 1, U_STD, "", HANA,
  "Koristiti CountAsync."),
 (ES, "70-77", "Izuzetak pri slanju e-maila se hvata sa catch (Exception) i samo ispiše na konzolu. Pozivalac ne zna da slanje nije uspjelo. Ubačeni _logger se uopšte ne koristi.",
  GRE, "Da li se greške ispravno zapisuju u interne logove?", 3, U_KOD, "CWE-390 / A09:2025", HANA,
  "Koristiti _logger.LogError i proslijediti grešku pozivaocu."),
 (ES, "24, 28, 41, 45", "Baca se generički tip System.Exception sa nejasnom porukom ('Null FromEmail').",
  GRE, "Da li se koriste odgovarajući tipovi izuzetaka?", 2, U_STD, "CWE-397", HANA,
  "Baciti InvalidOperationException sa jasnom porukom."),
 (ES, "53-66", "SMTP server i port su upisani direktno u kod. MailMessage implementira IDisposable, a ne oslobađa se.",
  DEF, "Are all files and devices left in the correct state upon program termination?", 2, U_KOD, "", HANA,
  "Dodati using za MailMessage; server i port premjestiti u konfiguraciju."),
 (ES, "20-51", "Dvije preopterećene metode SendEmailAsync ponavljaju iste provjere. Metoda Execute je javna, iako je pomoćna.",
  STR, "Are there any blocks of repeated code that could be condensed into a single procedure?", 1, U_KOD, "", HANA,
  "Prva metoda neka poziva drugu; Execute označiti kao private."),
 ("Data/ApplicationUser.cs", "1-6", "Klasa ApplicationUser se nigdje ne koristi (aplikacija koristi Korisnik). U .csproj je ostala i stavka Compile Remove za nepostojeći fajl.",
  STR, "Are there any uncalled or unneeded procedures or any unreachable code?", 1, U_TST, "", HANA,
  "Obrisati klasu i stavku iz .csproj."),
 ("Models/*.cs, " + DB, "svi modeli; DbContext 13-25", "Javna svojstva, DbSet-ovi i metode pisani su malim početnim slovom (id, naziv, korisnici, promjeniStanjePretplate), suprotno C# konvenciji. Nijedna klasa nema dokumentacijske komentare.",
  DOK, "Does the code conform to any pertinent coding standards?", 1, U_STD, "", HANA,
  "Preimenovati u PascalCase (IDE1006) i dodati XML komentare."),
 (DB, "89-102", "Greške u kucanju u početnim podacima koje vidi korisnik: 'Poboljšava tekstur', 'poblojšavaju'.",
  DOK, "Are all comments consistent with the code?", 1, U_KOD, "", HANA,
  "Ispraviti tekst i dodati migraciju."),
 ("appsettings.json", "3", "U repozitoriju je konekcijski string produkcijske baze (server, naziv baze, administratorski korisnik). Lozinka je prazna, ali ostali podaci su otkriveni. TrustServerCertificate=True isključuje provjeru certifikata servera.",
  TAJ, "Ima li u kodu konekcijskih stringova ili drugih tajni?", 3, U_ZAH, "CWE-295 / A02:2025", HANA,
  "Konekcijski string držati u User Secrets ili varijabli okruženja; ukloniti TrustServerCertificate."),
 ("SakuraWeb.csproj", "15-26", "Paketi iste porodice imaju različite verzije (10.0.7 i 10.0.8). NuGet.Packaging, NuGet.Protocol i Web.CodeGeneration.Design su alati za razvoj koji idu u produkciju. PROVJERITI komandom dotnet list package --vulnerable.",
  ZAV, "Sadrži li projekat zastarjele ili nepotrebne vanjske biblioteke?", 2, U_STD, "A03:2025", HANA,
  "Ujednačiti verzije i ukloniti nepotrebne pakete."),
]

TIP = [2, 2, 2, 2, 2, 2, 2, 4, 3, 2, 3, 3, 3, 4, 3, 2, 2, 3, 3, 2, 3, 3, 3, 4, 4, 3, 2, 3, 2, 2, 3, 2, 3, 4, 4, 4, 2, 2, 2, 4, 4, 5, 2, 2, 2, 2, 3, 2, 3, 2, 3, 3, 4, 3, 4, 3, 3, 4, 5, 5, 3, 4]
# Nalazi koje je tim potvrdio i objavio u pull requestu (ostali su odbačeni na sastanku).
ODABRANI = {1, 2, 4, 6, 8, 16, 19, 24, 25, 26, 27, 29, 30, 32, 34, 37, 40, 44, 45, 46, 50, 51, 52, 56, 58, 61}
# Konačni raspored: Hana je preuzela Anketa/Newsletter dio, Ilma (moderator) Usluga/Services/Data/konfiguraciju.
ZAMJENA = {ILMA: HANA, HANA: ILMA}
ISSUE = {1, 37}
rows = []
for i, n in enumerate(N, 1):
    f, l, o, k, s, sev, u, cwe, ins, p = n
    sev = TIP[i - 1]
    if i not in ODABRANI:
        continue
    ins = ZAMJENA.get(ins, ins)
    rows.append(dict(Issue="da" if i in ISSUE else "", ID=f"N{i:02d}", Fajl=f, Linije=l, Opis=o, Lista="Sigurnosna (Predavanje 3)" if k in SIG else "Generic Checklist for Code Reviews (Wiegers)",
                     Kategorija=k, Stavka=s, Ozbiljnost=sev, Uzrok=u, CWE_OWASP=cwe, Inspektor=ins, Prijedlog=p))

with open(f"{OUT}/nalazi.csv", "w", newline="", encoding="utf-8-sig") as fh:
    w = csv.DictWriter(fh, fieldnames=list(rows[0].keys())); w.writeheader(); w.writerows(rows)

SEVN = {1: "tip 1 – Kritična greška", 2: "tip 2 – Ozbiljna greška", 3: "tip 3 – Umjerena greška", 4: "tip 4 – Mala greška", 5: "tip 5 – Kozmetička greška"}

def pareto_tab(cnt):
    tot = sum(cnt.values()); cum = 0; out = []
    for k, v in sorted(cnt.items(), key=lambda kv: (-kv[1], kv[0])):
        cum += v; out.append((k, v, 100 * v / tot, 100 * cum / tot))
    return out

kat = Counter(r["Kategorija"] for r in rows)
uzr = Counter(r["Uzrok"] for r in rows)
sev = Counter(r["Ozbiljnost"] for r in rows)
ins = Counter(r["Inspektor"] for r in rows)

def chart(cnt, title, fn, wrap=16):
    import textwrap
    t = pareto_tab(cnt)
    labels = ["\n".join(textwrap.wrap(k, wrap)) for k, *_ in t]
    vals = [v for _, v, *_ in t]; cum = [c for *_, c in t]
    fig, ax = plt.subplots(figsize=(max(9, 0.95 * len(t) + 3), 5.6), dpi=150)
    fig.patch.set_facecolor("#fcfcfb"); ax.set_facecolor("#fcfcfb")
    ax.bar(range(len(t)), vals, width=0.62, color="#2a78d6")
    for i, v in enumerate(vals):
        ax.text(i, v + max(vals) * 0.015, str(v), ha="center", va="bottom", fontsize=9, color="#0b0b0b") if i else ax.text(i - 0.36, v + max(vals) * 0.015, str(v), ha="left", va="bottom", fontsize=9, color="#0b0b0b")
    ax.set_xticks(range(len(t))); ax.set_xticklabels(labels, fontsize=8, color="#52514e")
    ax.set_ylabel("Broj grešaka (komentara)", color="#52514e", fontsize=9)
    ax.set_ylim(0, sum(vals))
    ax2 = ax.twinx()
    ax2.plot(range(len(t)), cum, color="#0b0b0b", linewidth=2, marker="o", markersize=5)
    ax2.axhline(80, color="#52514e", linewidth=1, linestyle="--")
    ax2.text(len(t) - 0.55, 81.5, "80 %", fontsize=8, color="#52514e", ha="right")
    ax2.set_ylim(0, 100); ax2.set_ylabel("Kumulativni procenat (%)", color="#52514e", fontsize=9)
    for a in (ax, ax2):
        for s in ("top",): a.spines[s].set_visible(False)
        for s in a.spines.values(): s.set_color("#c9c8c2")
        a.tick_params(colors="#52514e", labelsize=8, length=0)
    ax.set_title(title, fontsize=12, color="#0b0b0b", loc="left", pad=14)
    fig.tight_layout(); fig.savefig(f"{OUT}/{fn}", facecolor=fig.get_facecolor()); plt.close(fig)

chart(kat, f"Pareto dijagram defekata po kategoriji liste provjere (ukupno {len(rows)})", "pareto_defekti.png")
chart(uzr, f"Pareto dijagram uzroka defekata (ukupno {len(rows)})", "pareto_uzroci.png", wrap=24)
tipc = Counter(SEVN[r["Ozbiljnost"]].split(" – ")[0].capitalize() for r in rows)
chart(tipc, f"Pareto dijagram broja komentara po tipu (ocjeni) greške (ukupno {len(rows)})", "pareto_tip_greske.png")

def md_tab(t, h):
    s = f"| {h} | Broj | Udio | Kumulativno |\n|---|---:|---:|---:|\n"
    for k, v, p, c in t: s += f"| {k} | {v} | {p:.1f} % | {c:.1f} % |\n"
    return s

# ---- Nalazi_inspekcije.md
md = ["# Nalazi inspekcije koda – SakuraWeb (ooad-2025-26/Grupa1-Tim4)\n",
      "Putanje su relativne u odnosu na `Implementacija/SakuraWeb/`. Brojevi linija odgovaraju commitu `fa92eda`.\n",
      "Tip greške je po dokumentu „Greške opisi“: 1 kritična, 2 ozbiljna, 3 umjerena, 4 mala, 5 kozmetička.\n",
      "Tekst u okviru ispod svakog nalaza je komentar u formatu iz uputa (opis, greška, checklista), objavljen u pull requestu.\n",
      "Oznake N01–N62 su iz radne liste pripreme; ovdje su samo nalazi koje je tim potvrdio i objavio.\n",
      "CWE i OWASP oznake prije objave provjeriti na cwe.mitre.org i owasp.org/Top10.\n"]
for who in (AMNA, AMINA, DZEVAD, HANA, ILMA):
    mine = [r for r in rows if r["Inspektor"] == who]
    md.append(f"\n## {who} ({len(mine)} nalaza)\n")
    for r in mine:
        md.append(f"### {r['ID']} – `{r['Fajl']}`, linije {r['Linije']}" + (" · issue kartica" if r['Issue'] else "") + "\n")
        md.append("```")
        md.append(f"{r['Opis']}")
        md.append(f"Greška: tip {r['Ozbiljnost']}.")
        md.append(f"Checklista: {r['Stavka']}" + (f" ({r['Kategorija']}, sigurnosna lista)" if r['Kategorija'] in SIG else ""))
        md.append("```")
        extra = f"- Kategorija: {r['Kategorija']}"
        if r["CWE_OWASP"]: extra += f" · {r['CWE_OWASP']}"
        md.append(extra)
        md.append(f"- Uzrok: {r['Uzrok']}")
        md.append(f"- Prijedlog ispravke: {r['Prijedlog']}\n")
open(f"{OUT}/Nalazi_inspekcije.md", "w", encoding="utf-8").write("\n".join(md))

# ---- statistika za izvjestaj
sev = Counter(r['Ozbiljnost'] for r in rows)
st = []
st.append(md_tab(pareto_tab(kat), "Kategorija defekta"))
st.append(md_tab(pareto_tab(uzr), "Uzrok"))
st.append(md_tab(pareto_tab(Counter(SEVN[r["Ozbiljnost"]] for r in rows)), "Tip greške"))
s = "| Inspektor | Broj nalaza |\n|---|---:|\n" + "".join(f"| {k} | {v} |\n" for k, v in ins.items())
st.append(s)
tab = "| ID | Fajl | Linije | Kategorija | Tip | Inspektor | Issue |\n|---|---|---|---|:-:|---|:-:|\n" + "".join(
    f"| {r['ID']} | `{r['Fajl']}` | {r['Linije']} | {r['Kategorija']} | {r['Ozbiljnost']} | {r['Inspektor']} | {'✔' if r['Issue'] else ''} |\n" for r in rows)
st.append(tab)
open(f"{OUT}/_stat.md", "w", encoding="utf-8").write("\n@@@\n".join(st))
print(len(rows), sorted(sev.items()))
for t in (2,):
    print('tip2:', [r['ID'] for r in rows if r['Ozbiljnost']==t])
print('sigtip2', sum(1 for r in rows if r['Ozbiljnost']==2 and r['Kategorija'] in SIG), 'PRI', sorted(Counter(r['Ozbiljnost'] for r in rows if r['Kategorija']==PRI).items()), 'STR', sorted(Counter(r['Ozbiljnost'] for r in rows if r['Kategorija']==STR).items()))
print('uzrok x tip2', Counter(r['Uzrok'] for r in rows if r['Ozbiljnost']==2)); print(pareto_tab(kat)); print(pareto_tab(uzr)); print(ins)
nsig = sum(1 for r in rows if r["Kategorija"] in SIG); print("sig", nsig)
