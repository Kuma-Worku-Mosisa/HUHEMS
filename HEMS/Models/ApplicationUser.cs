using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HEMS.Models
{
    public class ApplicationUser : IdentityUser
    {
        // PK (Id) is inherited from IdentityUser
        [MaxLength(20)]
        public string? Phone { get; set; } // [cite: 66]
        public DateTime CreatedAt { get; set; } = DateTime.Now; // [cite: 62]

        //[cite_start]// Relationship: One User to One Student [cite: 41]
        public virtual Student Student { get; set; }
    }
}