<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:output method="html" encoding="Windows-1252"/>
  
  <xsl:template match="t">
    <li><object type="text/sitemap">
      <xsl:element name="param">
        <xsl:attribute name="name">Name</xsl:attribute>
        <xsl:attribute name="value"><xsl:value-of select="@name"/></xsl:attribute>
      </xsl:element>
      <xsl:element name="param">
        <xsl:attribute name="name">Local</xsl:attribute>
        <xsl:attribute name="value"><xsl:value-of select="@href"/></xsl:attribute>
      </xsl:element>
    </object>
    <xsl:if test="t">
      <ul>
        <xsl:apply-templates select="t"/>
      </ul>
    </xsl:if>
    </li>
  </xsl:template>
  
  <xsl:template match="/toc" xml:space="preserve">
    <html>
      <body>
        <object type="text/site properties">
          <param name="Window Styles" value="0x800025"/>
          <param name="ImageType" value="Folder"/>
        </object>
        <ul>
          <xsl:apply-templates select="t"/>
        </ul>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="*|text()" />
  
</xsl:stylesheet> 
