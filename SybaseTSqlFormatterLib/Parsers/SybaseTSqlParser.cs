using System;
using System.Xml;
using PoorMansTSqlFormatterLib.Interfaces;
using PoorMansTSqlFormatterLib.Parsers;
using System.Collections.Generic;

namespace SybaseTSqlFormatterLib.Parsers
{
	public class SybaseTSqlParser : TSqlStandardParser
	{
        static readonly string DATE_FORMAT = "MM/dd/yyyy HH:mm:ss";

        static readonly string HEADER_BEGIN = "[/Formatter]";
        static readonly string HEADER_END = "[Formatter/]";
        static readonly string SQL_HEADER = " " + HEADER_BEGIN + " Formatted with Sybase T-SQL Formatter(version: {0}) at {1} {2}" + HEADER_END;

        static readonly string XPATH_ASTERISK = "//Asterisk[text()='*']";
        static readonly string XPATH_DATATYPE = "//DataTypeKeyword[preceding-sibling::*[1][self::WhiteSpace] and preceding-sibling::*[2][self::And|self::Or|self::Comma|self::OtherKeyword]]";
        static readonly string XPATH_EXTRA_LINES = "";

        static readonly string SIGN_EQUAL = "=";
        static readonly string SIGN_LEFT_JOIN = "*=";
        static readonly string SIGN_RIGHT_JOIN = "=*";

		static SybaseTSqlParser()
		{
			TSqlStandardParser.KeywordList.Clear();

			// only keep the following keywords:
			// ALTER,AS,ASC,BREAK,BY,CONTINUE,DROP,ELSE,EXEC,EXECUTE,FORCEPLAN,GROUP,HAVING,IS,NULL,ON,
			// ORDER,PROC,PROCEDURE,RETURN,TABLE,TRIGGER,TRUNCATE,UNION,VIEW,WHEN,WHERE
			TSqlStandardParser.KeywordList.Add("ALTER", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("AS", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("BREAK", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("BY", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("CONTINUE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("DELETE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("DROP", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("ELSE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("EXEC", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("EXECUTE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("EXISTS", KeywordType.OperatorKeyword);
			TSqlStandardParser.KeywordList.Add("FORCEPLAN", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("GROUP", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("HAVING", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("INTO", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("IS", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("NULL", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("ON", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("ORDER", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("PROCEDURE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("RETURN", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("TABLE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("TRIGGER", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("TRUNCATE", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("UNION", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("VIEW", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("WHEN", KeywordType.OtherKeyword);
			TSqlStandardParser.KeywordList.Add("WHERE", KeywordType.OtherKeyword);
		}

        private void RemoveUselessNodes(List<XmlNode> uselessNodes) {
            foreach (var node in uselessNodes)
            {
                node.ParentNode.RemoveChild(node);
            }

            uselessNodes.Clear();
        }

		public SybaseTSqlParser() : base()
		{
		}

		public override XmlDocument ParseSQL(ITokenList tokenList)
		{
            XmlDocument sqlTree = base.ParseSQL(tokenList);

            // post-process is more extensible although it's slow...
            if (sqlTree != null)
            {
                List<XmlNode> uselessNodes = new List<XmlNode>();

                // workaround for "*=" and "=*" support
                foreach (XmlNode n in sqlTree.SelectNodes(XPATH_ASTERISK))
                {
                    XmlNode x = n.NextSibling;
                    if (x != null && x.NodeType == XmlNodeType.Element 
                        && (x as XmlElement).Name == SqlXmlConstants.ENAME_EQUALSSIGN && x.InnerText == SIGN_EQUAL)
                    {
                        uselessNodes.Add(n);
                        x.InnerText = SIGN_LEFT_JOIN;
                        continue;
                    }

                    x = n.PreviousSibling;
                    if (x != null && x.NodeType == XmlNodeType.Element 
                        && (x as XmlElement).Name == SqlXmlConstants.ENAME_EQUALSSIGN && x.InnerText == SIGN_EQUAL)
                    {
                        uselessNodes.Add(n);
                        x.InnerText = SIGN_RIGHT_JOIN;
                    }
                }

                RemoveUselessNodes(uselessNodes);

                // workaround for incorrect data type recognition
                foreach (XmlNode n in sqlTree.SelectNodes(XPATH_DATATYPE))
                {
                    XmlElement xe = sqlTree.CreateElement(SqlXmlConstants.ENAME_OTHERNODE);
                    foreach (XmlNode xn in n.ChildNodes)
                    {
                        xe.AppendChild(xn);
                    }
                    n.ParentNode.ReplaceChild(xe, n);
                }

                // add heading comments
                XmlElement root = sqlTree.DocumentElement;
                XmlElement sqlClause = root.FirstChild as XmlElement;
                XmlElement firstChild = null;

                if (sqlClause != null && sqlClause.Name == SqlXmlConstants.ENAME_SQL_STATEMENT)
                {
                    foreach (XmlNode node in sqlClause.ChildNodes)
                    {
                        XmlElement el = node as XmlElement;
                        if (el == null || el.Name == SqlXmlConstants.ENAME_WHITESPACE)
                        {
                            uselessNodes.Add(node);
                        }
                        else if (el.Name == SqlXmlConstants.ENAME_COMMENT_SINGLELINE)
                        {
                            firstChild = el;
                            break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    RemoveUselessNodes(uselessNodes);
                }

                if (firstChild != null)
                {
                    string comment = firstChild.InnerText.Trim();
                    // [/Formatter] Formatted with Sybase T-SQL Formatter(ver: 1.4.3.16493) at 10-21-2013 15:47:18 +08:00 [Formatter/]
                    if (firstChild.Name == SqlXmlConstants.ENAME_COMMENT_SINGLELINE
                        && comment.StartsWith(HEADER_BEGIN)
                        && comment.EndsWith(HEADER_END))
                    {
                        //TODO: maybe parse the content and inform user something like "You're using an old version of SQL Formatter..."
                    }
                    else
                    {
                        firstChild = sqlTree.CreateElement(SqlXmlConstants.ENAME_COMMENT_SINGLELINE);
                        sqlClause.InsertBefore(firstChild, sqlClause.FirstChild);
                    }
                }
                else
                {
                    firstChild = sqlTree.CreateElement(SqlXmlConstants.ENAME_COMMENT_SINGLELINE);
                    if (sqlClause.HasChildNodes)
                    {
                        sqlClause.InsertBefore(firstChild, sqlClause.FirstChild);
                    }
                    else
                    {
                        sqlClause.AppendChild(firstChild);
                    }
                }

                DateTime currentDateTime = DateTime.Now;
                firstChild.InnerText = string.Format(SQL_HEADER,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    currentDateTime.ToString(DATE_FORMAT), TimeZone.CurrentTimeZone.GetUtcOffset(currentDateTime));

                // now remove extra lines before and after union/except/intersect keywords

            }

            return sqlTree;
		}
	}
}

