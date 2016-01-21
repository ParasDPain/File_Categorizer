using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnimeAssessor
{
    [TestClass]
    public class AnimeTvRepositoryTests
    {
        [TestMethod]
        public void OnlyTvItemsAreReturned()
        {
            var target = new AnimeTvRepository();

            var titles = target.GetTvTitles("TestDb.xml");

            Assert.AreEqual(5, titles.Count);

            Assert.AreEqual("Ketsuekigata-kun!", titles[0]);
            Assert.AreEqual("Kono Danshi, Maho ga Oshigoto Desu.", titles[1]);
            Assert.AreEqual("Ohenro", titles[2]);
            Assert.AreEqual("Qualidea Code", titles[3]);
            Assert.AreEqual("Tabi Machi Late Show", titles[4]);
        }
    }
}
