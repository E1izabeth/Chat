<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" exclude-result-prefixes="msxsl mstns #default"
                id="expandVisitorsTransform"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:xs="http://www.w3.org/2001/XMLSchema"
                xmlns:mstns="ChatSvcSpec"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt">

  <xsl:output method="text" omit-xml-declaration="yes" indent="yes" />

  <xsl:template match="@* | node()">
      <xsl:apply-templates select="@* | node()"/>
  </xsl:template>

  <xsl:template match="/xs:schema">
using System;

namespace Chat.Interaction.Xml
{
    <xsl:apply-templates />
}
  </xsl:template>

  <xsl:template match="/xs:schema/xs:complexType[@abstract='true' and count(./xs:complexContent/xs:extension)=0]">
    <xsl:variable name="baseName" select="./@name" />
    <xsl:variable name="visitorName" select="(concat('I', $baseName,'Visitor'))" />
    
  public interface <xsl:value-of select="$visitorName" />&lt;T&gt;
  {
    <xsl:call-template name="expandVisitorMethods">
      <xsl:with-param name="baseName" select="$baseName" />
    </xsl:call-template>
  }

  abstract partial class <xsl:value-of select="$baseName" />
  {
    public T Apply&lt;T&gt;(<xsl:value-of select="$visitorName" />&lt;T&gt; visitor)
    {
      return this.ApplyImpl&lt;T&gt;(visitor);
    }
      
    protected abstract T ApplyImpl&lt;T&gt;(<xsl:value-of select="$visitorName" />&lt;T&gt; visitor);
  }

    <xsl:call-template name="expandChildClasses">
      <xsl:with-param name="baseName" select="$baseName" />
      <xsl:with-param name="visitorName" select="$visitorName" />
  </xsl:call-template>
  </xsl:template>

  <xsl:template name="expandVisitorMethods">
    <xsl:param name="baseName" />
    <xsl:variable name="childs" select="/xs:schema/xs:complexType[(./xs:complexContent/xs:extension[@base=$baseName])]" />
    <xsl:for-each select="$childs" >
      <xsl:variable name="currName" select="./@name" />
      <xsl:if test="not(@abstract='true')">
    T Visit<xsl:value-of select="$currName" />(<xsl:value-of select="$currName" /> node);
      </xsl:if>
      <xsl:call-template name="expandVisitorMethods">
        <xsl:with-param name="baseName" select="$currName" />
      </xsl:call-template>
    </xsl:for-each>
  </xsl:template>
  
  <xsl:template name="expandChildClasses">
    <xsl:param name="baseName" />
    <xsl:param name="visitorName" />
    <xsl:variable name="childs" select="/xs:schema/xs:complexType[(./xs:complexContent/xs:extension[@base=$baseName])]" />
    <xsl:for-each select="$childs" >
      <xsl:variable name="currName" select="./@name" />
      <xsl:if test="not(@abstract='true')">
  partial class <xsl:value-of select="$currName" />
  {
    protected override T ApplyImpl&lt;T&gt;(<xsl:value-of select="$visitorName" />&lt;T&gt; visitor)
    {
      return visitor.Visit<xsl:value-of select="$currName" />(this);
    }
  }
      </xsl:if>
      <xsl:call-template name="expandChildClasses">
        <xsl:with-param name="baseName" select="$currName" />
        <xsl:with-param name="visitorName" select="$visitorName" />
      </xsl:call-template>
    </xsl:for-each>
  </xsl:template>

  <xsl:template match="/xs:schema/xs:complexType[.//xs:element[@type='DateTime'] | .//xs:attribute[@type='DateTime']]">
    <xsl:variable name="className" select="./@name" />
  partial class <xsl:value-of select="$className" />
  {
      <xsl:for-each select=".//xs:element[@type='DateTime'] | .//xs:attribute[@type='DateTime']">
        <xsl:call-template name="expandDateTimeProperty" />
      </xsl:for-each>
  }
  </xsl:template>

  <xsl:template name="expandDateTimeProperty">
    <xsl:variable name="ticksPropName" select="(concat('this.', ./@name,'Ticks'))" />
    <xsl:variable name="ticksSpecifiedPropName" select="(concat('this.', ./@name,'TicksSpecified'))" />
    <xsl:variable name="thisPropName" select="./@name" />
    <xsl:choose>
      <xsl:when test="./@use='required' or name(.)='xs:element'">
    public DateTime <xsl:value-of select="$thisPropName"/>
    {
        get { return new DateTime(<xsl:value-of select="$ticksPropName"/>); }
        set { <xsl:value-of select="$ticksPropName"/> = value.Ticks; }
    }
      </xsl:when>
      <xsl:when test="./@use='prophibited'">
      </xsl:when>
      <xsl:otherwise> <!-- no use attribute or optional -->
    public DateTime? <xsl:value-of select="$thisPropName"/>
    {
        get { return <xsl:value-of select="$ticksSpecifiedPropName"/> ? new DateTime(<xsl:value-of select="$ticksPropName"/>) : default(DateTime); }
        set { <xsl:value-of select="$ticksSpecifiedPropName"/> = value.HasValue; if (value.HasValue) <xsl:value-of select="$ticksPropName"/> = value.Value.Ticks; }
    }
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
