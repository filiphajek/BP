## Spuštění projektu
Předpokádá se, že kořenovým adresářem je adresář ```BP``` vytvořený podle přílohy práce.
V tomto adresáři se spouští další příkazy.\
Před spuštěním projektu je nutné mít HTTPS certifikát ve složce ```https```, odkud se
montuje do kontejneru. Vývojářský cerifikát lze vytvořit přes:

* Stažení .NET 6 SDK https://dotnet.microsoft.com/download/dotnet/6.0.
* V kořenovém adresáři zadat příkaz ```dotnet dev-certs https -ep https/aspnetapp.pfx -p mypass123```
* A poté zadat příkaz ```dotnet dev-certs https --trust```

Dále je potřeba vytvořit testovací image, který bude spouštěn a bude simulovat výpočet.
V kořenovém adresáři spusťte příkaz: ```docker build -t testimage -f TestImage/Dockerfile .```

Celý projekt se spustí přes ```docker-compose -f docker-compose.yml up --build -d```.
Výsledkem by měly být čtyři spuštěné docker kontejnery.\
Na této adrese <https://localhost:5001/> je dostupná webová aplikace.\
Na této adrese <https://localhost:5001/swagger/index.html> je dostupná OpenApi dokumentace.
Přes ```docker-compose -f docker-compose.yml down``` se kontejnery vypnou a smažou.

Spuštění projektu vytvoří tyto uživatele. Heslo je u všech účtů ```Password123*```.

| Uživatelský email            | Popis                                                          |
|------------------------------|----------------------------------------------------------------|
| admin@email.com              | Administrátor                                                  |
| tomashavel@test.com          | VIP, ověřený a registrovaný uživatel                           |
| filipnovak@test.com          | Ověřený a registrovaný uživatel                                |
| stepannemec@test.com         | VIP (nějaký čas VIP neměl), ověřený a registrovaný uživatel    |
| jakubstefacek@test.com       | Neověřený a neregistrovaný uživatel                            |

* **Ověřený** - Ověřený přes e-mail
* **Neregistrovaný** - Uživatel je přihlášen k Auth0, ale nedokončil registraci do aplikace

Detaily konfigurace služby jsou v příloze textu.

## Spuštění simulace
Simulace se spouští přes ```docker-compose -f docker-compose.simulation.yml up --build -d```.
Opět je třeba mít certifikát a testovací image.\
Další informace jsou v příloze textu.

## Nasazení workera
Pokud se služba nasadila na Azure pod jménem ```testauth0blazorwasmserverapp```, stačí 
workera spustit přes ```docker-compose -f docker-compose.worker.yml up -d```. Worker a testovací
image se stáhnou z Docker Hub.
