## Spu�ten�
Nejd��ve je pot�eba vytvo�it testovac� image, kter� bude spou�t�n a bude simulovat v�po�et.
V ko�enov�m adres��i projektu zadejte: ```docker build -t bptest -f .\TestImage\Dockerfile .```

Pot� cel� projekt spust�te p�es ```docker-compose up --build```. Tento proces m��e trvat 
n�kolik minut. V�sledkem by m�ly b�t �ty�i spu�t�n� docker kontejnery.
Na t�to adrese https://localhost:7001/ je dostupn� frontend aplikace.
Na t�to adrese https://localhost:8001/swagger/index.html je dostupn� webov� api (OpenApi dokumentace).

![image info](./Images/kontejnery.jpg)

V projektu zat�m pou��v�m v�voja�sk� certifik�t vygenerovan� p�es .NET CLI, aby fungovalo HTTPS.
Je mo�n�, �e se vyskytne chyba souvisej�c� s t�mto certifik�tem (typicky SSL vyj�mka, certifik�t nen� d�v�ryhodn� apod.).
Projekt tak nebude fungovat a certifik�t se bude muset vygenerovat nov�.

Pro vytvo�en� a ov��en� v�voja�sk�ho certifik�tu jsou pot�eba tyto kroky:
* Sta�en� .NET 6 SDK https://dotnet.microsoft.com/download/dotnet/6.0
* P�ej�t do hlavn�ho adres��e a zadat ```dotnet dev-certs https -ep .\https\aspnetapp.pfx -p mypass123```
* Potom v�e restartujte: ```docker-compose down```, ```docker-compose up --build```.

## Kr�tk� popis
Projekt je implementov�n v .NET 6 frameworku. D�le se pou��v� slu�ba 
Google bucket storage pro ukl�d�n� u�ivatelsk�ch soubor� a slu�ba Auth0 
pro autentizaci a autorizaci.

### Architektura:
Projekt je rozd�len na �ty�i ��sti (vzniknou �ty�i docker image).
1. Web Api
2. SQL Datab�ze
3. Frontend aplikace
4. Aplikace spou�t�j�c� kontejnery

#### Web Api
Obsluhuje datab�zi a komunikuje se slu�bou Google bucket storage pro ukl�d�n� soubor�.
Pro p��stup na toto Api je pot�eba se autorizovat pomoc� JWT Bearer. Ten klientsk�
aplikace z�skaj� posl�n�m dotazu na Auth0 spolu se soukrom�mi �daji. 

#### Frontend aplikace
Frontend aplikace je implementov�na pomoc� technologie Blazor .NET Core ASP.NET Hosted, kter� umo��uje architekturu BFF (Backend for frontend).
C�lem t�to architektury je v�t�� bezpe�nost a mo�nost implementace p�ihl�ov�n� p�es OpenIdConnect. D�ky tomu, �e ��st aplikace je na serveru,
informace jako ```clientId``` a ```clientSecret``` se nikdy nedostanou do prohl�e, stejn� jako autoriza�n� tokeny. Pokud klientsk�
aplikace pot�ebuje prov�st HTTP dotaz na Web Api, po�le ho na tuto serverovou ��st, kde se p�id� autoriza�n� token a je p�eposl�n na Web Api.
P�ihla�ovan� do aplikace zaji��uje slu�ba Auth0 p�es OpenIdConnect.

Tento server d�le hostuje SignalR Hub pro real-time komunikaci mezi Frontend aplikac� a
aplikac�, kter� spou�t� v�po�ty. Tento SignalR Hub m��e b�t odd�len a existovat na jin�m
serveru (vytvo�il by se dal�� docker image).

#### Aplikace spou�t�j�c� kontejnery
Aplikace m� za �kol spou�t�t v�po�ty. Jakmile do aplikace za�nou p�ich�zet zpr�vy (ze SignalR Hub) obsahuj�c� informaci o zah�jen� v�po�tu, 
za�nou se hromadit do fronty. Aplikace pak postupn� spou�t� v�po�et za v�po�tem dokud nen� fronta pr�zdn�, jinak aplikace �ek� a� p�ijde dal��
zpr�va o zah�jen� dal��ho v�po�tu.
Ne� aplikace spust� v�po�et, tak v�dy st�hne soubor pomoc� dotazu na Web Api. N�sledn� spust� docker image s mountem k tomuto souboru. 
Jakmile v�po�et skon��, aplikace ulo�� v�sledn� soubor op�t pomoc� dotazu na Web Api. Mezit�m prob�h� n�kolik dotaz� na Web Api, kter� akualizuj� stav v�po�tu.
Sou�asn� se p�es SignalR pos�laj� real-time zpr�vy do frontend aplikace.

Aplikace zat�m funguje pouze tak, �e spust� testovac� docker image a pouze v�po�et simuluje. D�le se zat�m pou��v� fronta, ale do budoucna nebude nic
br�nit tomu, aby se v�po�ty spou�tely paraleln�.
