using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoConnect_Hanoi.Models;

// Đưa Enum ra ngoài class để dễ gọi ở View và Controller
public enum ItemCondittion
{
    New=1,
    LikeNew=2,
    UsedGood=3,
    UsedFair=4
}
