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

  <!-- ================================ -->

  <xsl:key name="stockmsgs" match="stockmsg" use="@id"/>

  <xsl:template match="stockmsg"></xsl:template>
  <xsl:template match="stockref">
    <xsl:apply-templates select="key('stockmsgs', @id)/*"/>
  </xsl:template>

  <!-- ================================ -->

  <xsl:template match="note">
    <div class="note">
      <p class="note-title">
        <xsl:if test="@title">
          <xsl:value-of select="@title" />
        </xsl:if>
        <xsl:if test="not(@title)">
          Note
        </xsl:if>
      </p>
      <xsl:apply-templates />
    </div>
  </xsl:template>
  
  <!-- ================================ -->

  <xsl:template match="toc">
    <table class="toc">
      <tr>
        <td>
          <div class="toc-header">Contents</div>
          <ul>
            <xsl:for-each select="../h2">
              <xsl:apply-templates select="." mode="toc"/>
            </xsl:for-each>
          </ul>
        </td>
      </tr>
    </table>
  </xsl:template>

  <xsl:template match="*" mode="toc">
    <li>
      <xsl:element name="a">
        <xsl:attribute name="href">#<xsl:value-of select="generate-id()"/></xsl:attribute>
        <xsl:number level="any" count="h2"/>
        <xsl:if test="self::h3|self::h4">
          <xsl:text>.</xsl:text>
          <xsl:number level="any" count="h3" from="h2"/>
          <xsl:if test="self::h4">
            <xsl:text>.</xsl:text>
            <xsl:number level="any" count="h4" from="h3"/>
          </xsl:if>
        </xsl:if>
        <xsl:text xml:space="preserve">. </xsl:text>
        <xsl:apply-templates/>
      </xsl:element>
      <xsl:if test="key('toc-prev', generate-id())">
        <ul>
          <xsl:for-each select="key('toc-prev', generate-id())">
            <xsl:apply-templates select="." mode="toc"/>
          </xsl:for-each>
        </ul>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="h1|h2|h3|h4">
    <xsl:copy>
      <xsl:element name="a">
        <xsl:attribute name="name">
          <xsl:value-of select="generate-id()"/>
        </xsl:attribute>
        <xsl:apply-templates/>
      </xsl:element>
    </xsl:copy>
  </xsl:template>

  <xsl:key name="toc-prev" match="h2" use="generate-id(preceding-sibling::h1[1])"/>
  <xsl:key name="toc-prev" match="h3" use="generate-id(preceding-sibling::h2[1])"/>
  <xsl:key name="toc-prev" match="h4" use="generate-id(preceding-sibling::h3[1])"/>

  <xsl:template match="featurelist">
    <table border="1" cellspacing="0" class="featurelist">
      <tr>
        <th>Detail</th>
        <th>Status</th>
      </tr>
      <xsl:for-each select="yes">
        <xsl:sort/>
        <tr>
          <td class="featurelist-yes">
            <xsl:apply-templates/>
          </td>
          <td class="featurelist-yes">
            Yes
          </td>
        </tr>
      </xsl:for-each>
      <xsl:for-each select="no">
        <xsl:sort/>
        <tr>
          <td class="featurelist-no">
            <xsl:apply-templates/>
          </td>
          <td class="featurelist-no">
            No
          </td>
        </tr>
      </xsl:for-each>
    </table>
  </xsl:template>
  
  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="comment()|processing-instruction()" />

</xsl:stylesheet>

