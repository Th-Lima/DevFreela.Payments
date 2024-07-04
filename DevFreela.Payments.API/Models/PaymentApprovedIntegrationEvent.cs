namespace DevFreela.Payments.API.Models
{
    public class PaymentApprovedIntegrationEvent
    {
        public int Id { get; set; }
        public string FullName { get; set; }

        public PaymentApprovedIntegrationEvent(int idProject, string fullName)
        {
            Id = idProject;
            FullName = fullName;
        }
    }
}
