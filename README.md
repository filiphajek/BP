## Spuštení
Nejdøíve je potøeba vytvoøit testovací image, kterı bude spouštìn a bude simulovat vıpoèet.
V koøenovém adresáøi projektu zadejte: ```docker build -t bptest -f .\TestImage\Dockerfile .```

Poté celı projekt spustíte pøes ```docker-compose up --build```. Tento proces mùe trvat 
nìkolik minut. Vısledkem by mìly bıt ètyøi spuštìné docker kontejnery.
Na této adrese https://localhost:7001/ je dostupná frontend aplikace.
Na této adrese https://localhost:8001/swagger/index.html je dostupné webové api (OpenApi dokumentace).

![image info](./Images/kontejnery.jpg)

V projektu zatím pouívám vıvojaøskı certifikát vygenerovanı pøes .NET CLI, aby fungovalo HTTPS.
Je moné, e se vyskytne chyba související s tímto certifikátem (typicky SSL vyjímka, certifikát není dùvìryhodnı apod.).
Projekt tak nebude fungovat a certifikát se bude muset vygenerovat novı.

Pro vytvoøení a ovìøení vıvojaøského certifikátu jsou potøeba tyto kroky:
* Staení .NET 6 SDK https://dotnet.microsoft.com/download/dotnet/6.0
* Pøejít do hlavního adresáøe a zadat ```dotnet dev-certs https -ep .\https\aspnetapp.pfx -p mypass123```
* Potom vše restartujte: ```docker-compose down```, ```docker-compose up --build```.

## Krátkı popis
Projekt je implementován v .NET 6 frameworku. Dále se pouívá sluba 
Google bucket storage pro ukládání uivatelskıch souborù a sluba Auth0 
pro autentizaci a autorizaci.

### Architektura:
Projekt je rozdìlen na ètyøi èásti (vzniknou ètyøi docker image).
1. Web Api
2. SQL Databáze
3. Frontend aplikace
4. Aplikace spouštìjící kontejnery

#### Web Api
Obsluhuje databázi a komunikuje se slubou Google bucket storage pro ukládání souborù.
Pro pøístup na toto Api je potøeba se autorizovat pomocí JWT Bearer. Ten klientské
aplikace získají posláním dotazu na Auth0 spolu se soukromımi údaji. 

#### Frontend aplikace
Frontend aplikace je implementována pomocí technologie Blazor .NET Core ASP.NET Hosted, kterı umoòuje architekturu BFF (Backend for frontend).
Cílem této architektury je vìtší bezpeènost a monost implementace pøihlášování pøes OpenIdConnect. Díky tomu, e èást aplikace je na serveru,
informace jako ```clientId``` a ```clientSecret``` se nikdy nedostanou do prohlíe, stejnì jako autorizaèní tokeny. Pokud klientská
aplikace potøebuje provést HTTP dotaz na Web Api, pošle ho na tuto serverovou èást, kde se pøidá autorizaèní token a je pøeposlán na Web Api.
Pøihlašovaní do aplikace zajišuje sluba Auth0 pøes OpenIdConnect.

Tento server dále hostuje SignalR Hub pro real-time komunikaci mezi Frontend aplikací a
aplikací, která spouští vıpoèty. Tento SignalR Hub mùe bıt oddìlen a existovat na jiném
serveru (vytvoøil by se další docker image).

#### Aplikace spouštìjící kontejnery
Aplikace má za úkol spouštìt vıpoèty. Jakmile do aplikace zaènou pøicházet zprávy (ze SignalR Hub) obsahující informaci o zahájení vıpoètu, 
zaènou se hromadit do fronty. Aplikace pak postupnì spouští vıpoèet za vıpoètem dokud není fronta prázdná, jinak aplikace èeká a pøijde další
zpráva o zahájení dalšího vıpoètu.
Ne aplikace spustí vıpoèet, tak vdy stáhne soubor pomocí dotazu na Web Api. Následnì spustí docker image s mountem k tomuto souboru. 
Jakmile vıpoèet skonèí, aplikace uloí vıslednı soubor opìt pomocí dotazu na Web Api. Mezitím probíhá nìkolik dotazù na Web Api, které akualizují stav vıpoètu.
Souèasnì se pøes SignalR posílají real-time zprávy do frontend aplikace.

Aplikace zatím funguje pouze tak, e spustí testovací docker image a pouze vıpoèet simuluje. Dále se zatím pouívá fronta, ale do budoucna nebude nic
bránit tomu, aby se vıpoèty spouštely paralelnì.
