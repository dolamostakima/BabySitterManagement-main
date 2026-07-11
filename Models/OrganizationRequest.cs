namespace SmartBabySitter.Models
{
    public class OrganizationRequest
    {
        public int Id { get; set; }
        public string OrgName { get; set; }
        public string RegNo { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string House { get; set; }
        public string Road { get; set; }
        public string Area { get; set; }
        public string District { get; set; }
        public string Zip { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
