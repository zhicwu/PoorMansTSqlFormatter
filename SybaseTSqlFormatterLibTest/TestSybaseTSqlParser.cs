using NUnit.Framework;
using System;
using System.Text;
using System.Xml;
using PoorMansTSqlFormatterLib.Tokenizers;
using SybaseTSqlFormatterLib.Parsers;

namespace SybaseTSqlFormatterLibTest
{
    [TestFixture()]
    public class TestSybaseTSqlParser
    {
        private string ExtractInnerText(XmlDocument xml)
        {
            StringBuilder text = new StringBuilder();

            foreach (XmlNode node in xml.SelectNodes("//text()")) {
                text.Append(node.InnerText);
            }

            return text.ToString().Trim();
        }

        [Test()]
        public void TestParseSQLWithDateTypeKeywords()
        {
            var sql = @"select a.a, b.int, dAte, b.k1 as date, v.c as varchar from iNt..a, date..b, varchar v where a.Int=b.daTe";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(0, xml.SelectNodes("//DataTypeKeyword").Count);
        }

        [Test()]
        public void TestParseSQLWithLeftRightJoins()
        {
            var sql = @"select a.a1, b.b1 from a, b where (a.k1*=b.k1 and a.k2=*b.k2) or (a.k3 =* b.k3 and a.k4 *= b.k4)";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(0, xml.SelectNodes("//Asterisk").Count);
        }
    }
}

