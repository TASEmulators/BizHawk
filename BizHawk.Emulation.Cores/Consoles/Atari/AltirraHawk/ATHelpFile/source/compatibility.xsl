<?xml version='1.0' standalone='yes'?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:html="http://www.w3.org/1999/xhtml">
  <xsl:output method="html"/>

  <xsl:template match="/topic">
    <html>
      <head>
        <link rel="stylesheet" href="layout.css"/>
        <meta http-equiv="Content-Type" value="text/html; charset=utf-8"/>
        <title>
          Altirra Help: <xsl:value-of select="@title"/>
        </title>
        <style>
          table.compat {
            font-size: 10pt;
          }

          table.compat tr {
            background: #eee;
          }

          table.compat td.category {
            vertical-align: top;
            background: #edc;
          }
          
          table.compat td {
            vertical-align: top;
          }

          table.compat th {
            text-align: left;
            background: #eca;
          }
        </style>
      </head>
      <body>
        <div class="header">
          <div class="header-banner">Altirra Help</div>
          <div class="header-topic">
            <xsl:value-of select="@title"/>
          </div>
        </div>
        <div class="main">
          <xsl:apply-templates/>
        </div>
      </body>
    </html>
  </xsl:template>

  <xsl:key name="stockmsgs" match="compat-tag" use="@id"/>

  <xsl:template match="compat-tag"></xsl:template>
  <xsl:template match="compat-issue">
    <xsl:choose>
      <xsl:when test="*">
        <xsl:apply-templates/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="key('stockmsgs', @id)/inline-text/*"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="compat-toc">
    <table class="compat">
      <tr>
        <th>
          Category
        </th>
        <th>
          Name
        </th>
        <th>
          Issues
        </th>
      </tr>
      <xsl:for-each select="//compat-category">
        <xsl:sort select="@name"/>
        <xsl:for-each select="compat-title">
          <xsl:sort select="@name"/>
          <tr>
            <xsl:if test="position()=1">
              <xsl:element name="td">
                <xsl:attribute name="class">
                  category
                </xsl:attribute>
                <xsl:attribute name="rowspan">
                  <xsl:value-of select="count(..//compat-title)"/>
                </xsl:attribute>
                <xsl:attribute name="valign">
                  top
                </xsl:attribute>
                <xsl:element name="a">
                  <xsl:attribute name="href">#<xsl:value-of select="generate-id(..)"/></xsl:attribute>
                  <xsl:value-of select="../@name"/>
                </xsl:element>
              </xsl:element>
            </xsl:if>
            <td>
              <xsl:element name="a">
                <xsl:attribute name="href">#<xsl:value-of select="generate-id(.)"/></xsl:attribute>
                <xsl:value-of select="@name"/>
              </xsl:element>
            </td>
            <td>
              <xsl:for-each select="compat-issue">
                <xsl:variable name="id" select="@id"/>
                <xsl:choose>
                  <xsl:when test="@id">
                    <xsl:value-of select="//compat-tag[@id=$id]/@name" />
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:value-of select="@desc" />
                  </xsl:otherwise>
                </xsl:choose>
                <br/>
              </xsl:for-each>
            </td>
          </tr>
        </xsl:for-each>
      </xsl:for-each>
    </table>
  </xsl:template>
  
  <xsl:template match="compat-tags">
    <!--
    <xsl:for-each select="compat-tag">
      <xsl:sort select="@name"/>
      <h3>
        <xsl:element name="a">
          <xsl:attribute name="name">
            <xsl:value-of select="generate-id()"/>
          </xsl:attribute>
          <xsl:value-of select="@name" />
        </xsl:element>
      </h3>
      <xsl:apply-templates select="desc" />
    </xsl:for-each>
    -->
  </xsl:template>
  
  <xsl:template match="compat-list">
    <xsl:for-each select="compat-category">
      <xsl:sort select="@name"/>
      <xsl:apply-templates select="."/>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="compat-category">
    <h2>
      <xsl:element name="a">
        <xsl:attribute name="name">
          <xsl:value-of select="generate-id()"/>
        </xsl:attribute>
        <xsl:value-of select="@name"/>
      </xsl:element>
    </h2>
    <xsl:for-each select="compat-title">
      <xsl:sort select="@name"/>
      <xsl:apply-templates select="."/>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="compat-title">
    <h3>
      <xsl:element name="a">
        <xsl:attribute name="name">
          <xsl:value-of select="generate-id()"/>
        </xsl:attribute>
        <xsl:value-of select="@name"/>
      </xsl:element>
    </h3>
    <xsl:apply-templates/>
  </xsl:template>
  
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="comment()|processing-instruction()" />

</xsl:stylesheet>
