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
        public void TestParseSqlWithDateTypeKeywords()
        {
            var sql = @"select a.a, b.int, dAte, b.k1 as date, v.c as varchar from iNt..a, date..b, varchar v where a.Int=b.daTe";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(0, xml.SelectNodes("//DataTypeKeyword").Count);
        }

        [Test()]
        public void TestParseSqlWithLeftRightJoins()
        {
            var sql = @"select a.a1, b.b1 from a, b where (a.k1*=b.k1 and a.k2=*b.k2) or (a.k3 =* b.k3 and a.k4 *= b.k4)";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(0, xml.SelectNodes("//Asterisk").Count);
        }

		[Test()]
		public void TestParseDataTypeDeclare() 
		{
			var sql = @"declare varchar(10) a";

			TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
			var tokenList = tokenizer.TokenizeSQL(sql);

			SybaseTSqlParser parser = new SybaseTSqlParser();
			XmlDocument xml = parser.ParseSQL(tokenList);

			Assert.AreEqual(2, xml.SelectNodes("//WhiteSpace").Count);
		}

        [Test()]
        public void TestParseSqlWithTxKeyword()
        {
            var sql = @"create proc test
as
         select count(*) 
         from CIS..order_header oh
         left outer join CIS..order_detail od
                   on oh.order_no=od.order_no and oh.order_type=od.order_type
         where b.order_type is null
         
         begin tran
         commit tran
return
";
            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(2, xml.SelectNodes("//OtherKeyword[text()='tran']").Count);
        }

        [Test()]
        public void TestParseSqlWithNameKeyword()
        {
            var sql = @"select name from sometable";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(1, xml.SelectNodes("//Other[text()='name']").Count);
        }

        [Test()]
        public void TestParseSqlWithInKeyword()
        {
            var sql = @"select a,b,c from k where a in ('a', 1, 'c')";

            TSqlStandardTokenizer tokenizer = new TSqlStandardTokenizer();
            var tokenList = tokenizer.TokenizeSQL(sql);

            SybaseTSqlParser parser = new SybaseTSqlParser();
            XmlDocument xml = parser.ParseSQL(tokenList);

            Assert.AreEqual(1, xml.SelectNodes("//AlphaOperator[text()='in']").Count);
        }
    }
}

