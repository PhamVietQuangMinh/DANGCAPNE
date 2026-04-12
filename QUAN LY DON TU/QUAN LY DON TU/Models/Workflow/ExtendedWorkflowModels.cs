using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.Workflow
{
    public class WorkflowRoutingRule
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int WorkflowId { get; set; }
        public int? StepId { get; set; }
        [MaxLength(20)]
        public string RouteType { get; set; } = "Sequential"; // Sequential, Parallel
        [MaxLength(50)]
        public string? ParallelGroupCode { get; set; }
        public int MinApprovalsRequired { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("WorkflowId")]
        public virtual WorkflowDef? Workflow { get; set; }
        [ForeignKey("StepId")]
        public virtual WorkflowStep? Step { get; set; }
    }
}
