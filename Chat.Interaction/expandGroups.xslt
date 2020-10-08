<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" exclude-result-prefixes="msxsl mstns #default"
                id="expandGroupsTransform"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
                xmlns:mstns="ChatSvcSpec"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt">

  <xsl:output method="xml" indent="yes" />

  <xsl:template match="*">
    <xsl:element name="{concat('xs:',local-name())}" >
      <xsl:for-each select="@*">
        <xsl:attribute name="{local-name()}">
          <xsl:value-of select="."/>
        </xsl:attribute>
      </xsl:for-each>
      <xsl:if test="count(./*)!=0">
        <xsl:apply-templates/>
      </xsl:if>
    </xsl:element>
  </xsl:template>

  <xsl:template match="/xs:schema" >
    <xs:schema id="ChatSvc" elementFormDefault="qualified" targetNamespace="ChatSvc" xmlns="ChatSvc" xmlns:mstns="ChatSvc">
      <xsl:apply-templates />
    </xs:schema>
  </xsl:template>

  <xsl:template match="//xs:attribute[@type='DateTime']">
    <xsl:variable name="attrName" select="(concat(./@name,'Ticks'))" />
    <xsl:element name="xs:attribute">
      <xsl:copy-of select="./@*[not(name()='xs:name') and not(name()='xs:type')]" />
      <xsl:apply-templates />
      <xsl:attribute name="name">
        <xsl:value-of select="$attrName"/>
      </xsl:attribute>
      <xsl:attribute name="type">
        <xsl:value-of select="'long'"/>
      </xsl:attribute>
    </xsl:element>
  </xsl:template>
  
  <xsl:template match="//xs:element[@type='DateTime']">
    <xsl:variable name="attrName" select="(concat(./@name,'Ticks'))" />
    <xsl:element name="xs:element">
      <xsl:copy-of select="./@*[not(name()='xs:name') and not(name()='xs:type')]" />
      <xsl:apply-templates />
      <xsl:attribute name="name">
        <xsl:value-of select="$attrName"/>
      </xsl:attribute>
      <xsl:attribute name="type">
        <xsl:value-of select="'long'"/>
      </xsl:attribute>
    </xsl:element>
  </xsl:template>
  
  <xsl:template match="//xs:complexType//xs:group">
    <xsl:variable name="groupName" select="./@ref" />
    <xsl:apply-templates select="/xs:schema/xs:group[@name=$groupName]/*" />
  </xsl:template>

</xsl:stylesheet>
