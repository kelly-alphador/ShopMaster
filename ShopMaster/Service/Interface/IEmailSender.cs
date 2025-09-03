namespace ShopMaster.Service.Interface
{
    public interface IEmailSender
    {
        //Email c'est email des personnes qui va confirmer son compte
        //subject c'est l'objet 
        //message c'est le message envoye
        Task SendEmailAsync(string email,string subject,string message);
    }
}
