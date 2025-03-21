using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

public class DatabaseOptions
{
    [Required]
    public string ConnectionString { get; set; } = null!;
}
