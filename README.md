## Úvod
Projekt se zabývá implementací internetové služby, která poskytuje uživatelům spouštět 
úlohy. Úloha se spouští jako docker kontejner.

Služba je implementována ve frameworku .NET 6. Dále se používá služba 
Google bucket storage pro ukládání uživatelských souborů a služba Auth0 
pro autentizaci a autorizaci.

## Spuštění projektu
Projekt se spouští přes docker-compose. Nejdříve je ale potřeba vytvořit testovací image, který bude spouštěn a bude simulovat výpočet.
V kořenovém adresáři spusťte příkaz: ```docker build -t testimage -f .\TestImage\Dockerfile .```

Poté celý projekt spustíte přes ```docker-compose up --build```. Tento proces může trvat 
několik minut. Výsledkem by měly být čtyři spuštěné docker kontejnery.\
Na této adrese <https://localhost:5001/> je dostupná webová aplikace.\
Na této adrese <https://localhost:5001/swagger/index.html> je dostupná OpenApi dokumentace.

![image info](./Images/kontejnery.JPG)

V takto spuštěném projektu se používá vývojářský certifikát vygenerovaný přes .NET CLI, aby fungoval provoz přes HTTPS.
Je možné, že se vyskytne chyba související s tímto certifikátem (typicky SSL výjimka, certifikát není důvěryhodný apod.).
Projekt tak nebude fungovat a certifikát se bude muset vygenerovat nový.

Pro vytvoření a ověření vývojářského certifikátu jsou potřeba tyto kroky:
* Stažení .NET 6 SDK https://dotnet.microsoft.com/download/dotnet/6.0
* Přejít do hlavního adresáře projektu a zadat ```dotnet dev-certs https -ep .\https\aspnetapp.pfx -p mypass123```
* Potom vše restartujte příkazy: ```docker-compose down```, ```docker-compose up --build```.

Poté by mělo vše fungovat správně. Na adrese <https://localhost:5001/> se vpravo nahoře můžete přihlásit
nebo zaregistrovat. Zde je tabulka již existujícíh uživatelů. U všech je heslo
nastavené na ```Password123*```.

| Uživatelský email            | Popis                                                   |
|------------------------------|---------------------------------------------------------|
| testadmin@example.com        | Administrator                                           |
| tomashavel@test.com          | Vip, ověřený, registrovaný uživatel                     |
| filipnovak@test.com          | Normální, ověřený, registrovaný uživatel                |
| stepannemec@test.com         | Vip (byl i normální), ověřený, registrovaný uživatel    |
| jakubstefacek@test.com       | Normální, neověřený, neregistrovaný uživatel            |
| testadmin@example.com        | Normální, ověřený, neregistrovaný uživatel              |

Vysvětlení popisu:
* **Ověřený** - Ověřený přes e-mail
* **Registrovaný** - Uživatel je přihlášen k auth0, ale nedokončil registraci do aplikace

## Konfigurace projektu
Projekt lze konfigurovat přes kongirurační proměnné v docker compose souboru.
Konfigurační proměnné jsou uložené v souborech ```appsettings.json```. Tyto proměnné
lze v docker compose souboru změnit.

Konfigurační soubor ```TaskLauncher.App/Server/appsettings.json``` slouží ke konfiguraci
serveru. Lze měnit nastavení poskytovatele idenity, připojovací řetězec k databázi,
váhy front úloh a podobně.

Hodnoty, které se mohou měnit a neovlivní zásadně chod serveru jsou:
* **SeederConfig__seed** Pokud je nastaveno na ```true```, vytvoří se testovací uživatelé s testovacími daty (pouze pokud je prázdná databáze). 
Pokud je hodnota nastavena ```false```, je tato funkce vypnuta.
* **PriorityQueues__Queues__nonvip** Určuje jakou váhu má nastavená fronta s normálními úlohy.
* **PriorityQueues__Queues__vip** Určuje jakou váhu má nastavená fronta s priotitními úlohy.
* **PriorityQueues__Queues__cancel** Určuje jakou váhu má nastavená fronta s úlohy, které nečekaně selhaly z důvodu odpojení workera nebo nečekané vyjímky.

Další konfigurace je ```TaskLauncher.Routines/appsettings.json```. Zde by se nic měnit nemělo.
Konfigurace aplikaci, která vykonává rutinní práce.

Poslední konfigurační soubor je ```TaskLauncher.Worker/appsettings.json```. Ten konfiguruje worker aplikaci, která
spouští kontejner simlující výpočet.

Hodnoty, které se zde mohou měnit:
* **TaskLauncherConfig__Target** Určuje cestu, kam worker bude ukládat soubor. Nedoporučuje se měnit tuto proměnnou.
* **TaskLauncherConfig__Source** Toto je jméno volume. Stejný volume by měl být přimontován jak k worker kontejneru, tak k workerem spuštěnému kontejneru.
* **TaskLauncherConfig__ImageName** Jméno testovacího image.
* **TaskLauncherConfig__ContainerArguments__Mode** Zde jsou možné pouze dvě možnosti: "seconds", "minutes". Určuje jednotku času v dalších proměnných.
* **TaskLauncherConfig__ContainerArguments__Min** Minimální čas, po který poběží spuštěný kontejner s výpočtem. Jednotku určuje proměnná Mode.
* **TaskLauncherConfig__ContainerArguments__Max** Maximální čas, po který poběží spuštěný kontejner s výpočtem. Jednotku určuje proměnná Mode.
* **TaskLauncherConfig__ContainerArguments__Max** Určuje šanci na úspěch úlohy (zadávejte jako desetinné číslo od 0 do 1 s tečkou).
 
## Simulace
Spuštění simulace probíhá opět pomocí docker-compose. Tentokrát se v příkazu uvede 
soubor s konfigurací pro simulaci ```docker-compose -f .\docker-compose.simulation.yml up --build```.

Simulace vytvoří několik normálních a několik vip uživatelů. Ti pak vytvoří několik tasků po nějaké době.
Všechny tyto hodnoty jsou konfigurovatelné. Pro nahlédnutí výsledku nebo fronty se přihlašte do aplikace
na adrese <https://localhost:5001/> jako administrátor nebo jako uživatel, který se v simulaci vygeneroval.
Heslo je vždy stejné: ```Password123*```. Email získáte z logu, který produkuje simulace.

## Konfigurace simulace
V simulaci se nespouští management aplikace, takže související proměnné nelze konfigovat.
Zbývající proměnné jsou stejné jako u standartního spuštění projektu.
K těmto proměnným přibyly proměnné z konfigurace v souboru ```TaskLauncher.Simulation/appsettings.json```

Hodnoty, které se mohou měnit a neovlivní zásadně chod aplikace jsou:
* **SimulationConfig__VipUsers** Nastavuje počet vygenerovaných vip uživatelů.
* **SimulationConfig__NormalUsers** Nastavuje počet standartních uživatelů.
* **SimulationConfig__TaskCount** Nastavuje počet tasků, který každý vytvořený uživatel nastaví.
* **SimulationConfig__DelayMin** Hastavuje spodní hranici hodnoty zpoždění v sekundách, kterou se bude čekat po vytvoření tasku.
* **SimulationConfig__DelayMax** Hastavuje horní hranici hodnoty zpoždění v sekundách, kterou se bude čekat po vytvoření tasku.

### Architektura:
Projekt je rozdělen na čtyři části (čtyři docker image).
* Webová aplikace
* SQL server databáze
* Management aplikace
* Aplikace spouštějící výpočty - Worker

### Schéma:
![image info](./Images/schema.jpg)

### Webová aplikace
Aplikace byla vytvořena pomocí frameworku ASP.NET Core 6. Dále se
využil model Blazor WebAssembly ASP.NET Core hosted, který dokáže hostovat webové stránky.
Vznikají tak dvě aplikace (resp. dvě části). Obě aplikace jsou hostovány na serveru Kestrel. Obě aplikace mají
stejnou doménu. Pokud se přistoupí na adresu https://{doména}/.. z prohlížeče. Otevřou se klasicky statické stránky.
Pokud se přistupuje na adresu https://{doména}/api/.. přistupuje se k REST API. Doména je v lokálním spuštění localhost:5001.
Po spuštění nicméně vzniká pouze jeden kontejner jako jedna aplikace, kterou je server Kestrel. Kestrel tak
poskytuje statické stránky a zároveň REST API.
Pokud by byl požadavek rozdělit tento model na více kontejnerů, je několik možnostní. 

1. REST API (ASP.NET core 6 backend) by zůstalo na serveru, statické stránky (SPA) by se mohly hostovat na jiné instanci
Kestrel serveru v jiném kontejneru pomocí modelu Blazor WebAssembly nebo na kompletně jiném serveru jako je Nginx.
2. REST API by se opět neměnilo a blazor aplikace by se nechala nasadit pomocí modelu Blazor Server.

Hostovací model Blazor WebAssembly ASP.NET Core hosted byl zvolen, kvůli jednoduššímu nasazování. 
Server Kestrel je schopný zvládat jak poskytování statických souborů, tak hostování backend aplikace zároveň.

Frontend aplikace komunikuje s REST API backend. Pro autentizaci a autorizaci se používá služba auth0.
Blazor aplikace se dále připojuje na SignalR hub, který je hostovaný společně s REST API, který zasílá
uživatelům notifikace. 
Backend aplikace obsluhuje databázi a komunikuje se službou Google bucket storage pro ukládání souborů.
Dále rozděluje úlohy worker aplikacím.
Pro přístup na REST API z prohlížeče se musí uživatel autentizovat a autorizovat pomocí cookie autentizace.
Autentizace a autorizace probíhá přes službu auth0. Uživatel se může k API dostat i z klasické desktop aplikace,
přes JWT access token, který získá přihlášením přes password login flow.

### Management aplikace
Tato aplikace má za úkol vykonávat rutinní práce jako mazání starých souborů. Aplikace byla implementována
pomocí balíčku Hangfire, který umí plánovat práce a vykonávat je po dané době. Pokud se objeví požadavek, například
mazat neaktivní uživatele apod. Lze rutinu naprogramovat a naplánovat ji do Hangfire.

### Aplikace spouštějící výpočty
Aplikace má za úkol spouštět výpočty. Jakmile do aplikace začnou přicházet zprávy (ze SignalR Hub) obsahující informaci o zahájení výpočtu, 
začnou se hromadit do fronty. Aplikace pak postupně spouští výpočet za výpočtem, dokud není fronta prázdná, jinak aplikace čeká až přijde další
zpráva o zahájení dalšího výpočtu.
Než aplikace spustí výpočet, tak vždy stáhne soubor pomocí dotazu na Web Api. Následně spustí docker image s mountem k tomuto souboru. 
Jakmile výpočet skončí, aplikace uloží výsledný soubor opět pomocí dotazu na Web Api. Mezitím probíhá několik dotazů na Web Api, které aktualizují stav výpočtu.
Současně se přes SignalR posílají real-time zprávy do frontend aplikace.

Aplikace zatím funguje pouze tak, že spustí testovací docker image a pouze výpočet simuluje. Dále se zatím používá fronta, ale do budoucna nebude nic
bránit tomu, aby se výpočty spouštěly paralelně.
