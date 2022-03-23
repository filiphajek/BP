namespace TaskLauncher.Common.Enums;

public enum TaskState
{
    Created, // task je zalozen a je ve fronte
    Ready, // task si prevzal nejaky worker
    Running, // worker zacina vykonavat task
    Cancelled, // uzivatel zrusil task - task se da zrusit jenom pokud je ve
               // fronte, pote se task smaze automaticky nebo ho muze smazat uzivatel, task se nemuze restartovat, musi se zalozit novy
               // vrati se tokeny za platbu
    Crashed, // znamena interni chybu (worker se necekane odpojil, spadnul) - task je okamzite restartovan, znovu zarazen do fronty s nejvyssi priritou
    Timeouted, // task se nejspise zacyklil, uzivatel muze tento task restartovat
    FinishedSuccess, // task se dokoncil uspesne
    FinishedFailure, // task se dokoncil ale s chybou
    Downloaded, // vysledek tasku byl stazen
    Closed, // uzivatel uzavrel task .. task bude smazan
}
