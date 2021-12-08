namespace TaskLauncher.Common.Configuration;

public class ServiceAddresses
{
    private string webApiaddress;
    public string WebApiAddress
    {
        get => webApiaddress; set
        {
            webApiaddress = value.Trim('/');
            WebApiAddressUri = new(webApiaddress);
        }
    }
    public Uri WebApiAddressUri { get; private set; }


    private string hubaddress;
    public string HubAddress
    {
        get => hubaddress; set
        {
            hubaddress = value.Trim('/');
            HubAddressUri = new(hubaddress);
        }
    }
    public Uri HubAddressUri { get; private set; }
}
