using System.IO;
using System.Threading.Tasks;

namespace CloneDroneELO
{
    public interface IAsyncSerializable
    {
        Task SerializeAsync(BinaryWriter binaryWriter);
        Task DeserializeAsync(BinaryReader binaryReader);
    }
}
