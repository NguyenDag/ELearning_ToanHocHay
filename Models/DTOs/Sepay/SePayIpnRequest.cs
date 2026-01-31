namespace ELearning_ToanHocHay_Control.Models.DTOs.Sepay
{
    public class SePayIpnRequest
    {
        public long Id { get; set; }
        public string? gateway { get; set; }
        public string? transactionDate { get; set; }
        public string? accountNumber { get; set; }
        public string? code { get; set; }
        public string? content { get; set; }
        public string? transferType { get; set; } // in | out
        public long transferAmount { get; set; }
        public long accumulated { get; set; }
        public string? subAccount { get; set; }
        public string? referenceCode { get; set; }
        public string? description { get; set; }
    }
}
