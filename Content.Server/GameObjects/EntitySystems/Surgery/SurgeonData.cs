using System.Threading;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Server.GameObjects.EntitySystems.Surgery
{
    public class SurgeonData
    {
        public SurgeonData(IBodyPart part, CancellationTokenSource? surgeryCancellation = null)
        {
            Part = part;
            SurgeryCancellation = surgeryCancellation;
        }

        public IBodyPart Part { get; }

        public CancellationTokenSource? SurgeryCancellation { get; }
    }
}
