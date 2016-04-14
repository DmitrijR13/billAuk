using System.Data;
using System.Windows.Forms;

namespace webroles
{
    interface INamePosition
    {
        string GetNamePosition( DataGridViewRow dataRow);
        int Position { get; set; }

        int PositionDefault { get;}
    }
}
