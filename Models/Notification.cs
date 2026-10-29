using System;
using System.ComponentModel.DataAnnotations;

namespace FitWiseBD.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string Type { get; set; } = "General";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}