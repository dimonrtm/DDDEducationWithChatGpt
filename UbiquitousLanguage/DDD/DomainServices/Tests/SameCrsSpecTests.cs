using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class SameCrsSpecTests
    {
        [Fact]
        public void SameCrs_returns_true()
        {
            var layer = new Layer(Guid.NewGuid(), new Crs("EPSG:4326"));
            var version = new LayerVersion(Guid.NewGuid(), layer.Id, new Crs("EPSG:4326"));
            var spec = new SameCrsSpec();
            var attempt = new PublicationAttempt(layer, version);

            Assert.True(spec.IsSatisfiedBy(attempt));
        }

        [Fact]
        public void DifferentCrs_returns_false()
        {
            var layer = new Layer(Guid.NewGuid(), new Crs("EPSG:3857"));
            var version = new LayerVersion(Guid.NewGuid(), layer.Id, new Crs("EPSG:4326"));
            var spec = new SameCrsSpec();
            var attempt = new PublicationAttempt(layer, version);

            Assert.False(spec.IsSatisfiedBy(attempt));
        }
    }
}
