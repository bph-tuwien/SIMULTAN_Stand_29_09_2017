<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
							  xmlns:n="http://DataContractSerializer.NodeList/"
							  xmlns:d2p1="http://DataContractSerializer.Node/"
                xmlns:z="http://schemas.microsoft.com/2003/10/Serialization/">
  <xsl:template match="n:Node_List/n:Items">
    <html>
      <head>
        <title>Node Collection</title>
        <link rel="stylesheet" type="text/css" href="export_style.css" />
        <script type="text/javascript" src="sorttable.js"></script>
      </head>
      <body>
        <h2>Node Collection</h2>
        <table class="sortable">
          <tr bgcolor="#dddddd">
            <th>ID</th>
            <th>Last Update</th>
            <th>Name</th>
            <th>Sync</th>
            <th>Param Type</th>
            <th>Descr</th>
            <th>Unit</th>
            <th>Default Value</th>
            <th>Source</th>
            <th>Manager</th>
            <th>Geometry</th>
            <th>Level</th>
          </tr>
		  <xsl:apply-templates />
        </table>
      </body>
    </html>
  </xsl:template>
  
  <xsl:template match="d2p1:Node">
	<xsl:variable name="ref0" select="@z:Ref"/>
    <xsl:variable name="id0" select="@z:Id"/>
    <xsl:for-each select="//d2p1:Node[$ref0 = @z:Id] | //d2p1:Node[$id0 = @z:Id]">
		<tr style="vertical-align:top">
                <td>
                  <xsl:value-of select="d2p1:ID"/>
                </td>
                <td>
                  <xsl:variable name="datetime" select="d2p1:LastEdit"/>
                  <xsl:variable name="date" select="substring-before($datetime, 'T')"/>
                  <xsl:variable name="time" select="substring-after($datetime, 'T')"/>
                  <xsl:variable name="YYYY" select="substring($date,9,2)"/>
                  <xsl:variable name="MM" select="substring($date,2,2)"/>
                  <xsl:variable name="DD" select="substring($date,1,4)"/>
                  <xsl:variable name="hh" select="substring($time,1,2)"/>
                  <xsl:variable name="mm" select="substring($time,4,2)"/>                  
                  <xsl:value-of select="concat($DD, '.', $MM, '.', $YYYY, ' ', $hh, ':', $mm)"/>
                </td>
                <td>
                  <xsl:choose>
                    <xsl:when test="d2p1:SyncByName = 'true'">
                      <span class="mainsynced level{count(ancestor::d2p1:Node)}">
                        <xsl:value-of select="d2p1:NodeName"/>
                      </span>
                    </xsl:when>
                    <xsl:otherwise>
                      <span class="mainnotsynced level{count(ancestor::d2p1:Node)}">
                        <xsl:value-of select="d2p1:NodeName"/>
                      </span>
                    </xsl:otherwise>
                  </xsl:choose>
                </td>
                <td>
                  <xsl:value-of select="d2p1:SyncByName"/>
                </td>
                <td>
                  <xsl:value-of select="d2p1:NodeParamType"/>
                </td>
                <td>
                  <xsl:value-of select="d2p1:NodeDescr"/>
                </td>
                <td>
                  <xsl:value-of select="d2p1:NodeUnit"/>
                </td>
                <td>
                  <xsl:value-of select="d2p1:NodeDefaultVal"/>
                </td>
                <td>
                  <xsl:value-of select="d2p1:NodeSource"/>
                </td>
                <xsl:choose>
                  <xsl:when test="d2p1:NodeManager = 'REGULATOR'">
                    <td style="background: #ff3600;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'MODERATOR'">
                    <td style="background: #8e1e00;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'ENERGY_NETWORK_OPERATOR'">
                    <td style="background: #a75a00;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'EENERGY_SUPPLIER'">
                    <td style="background: #ff8a00;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'BUILDING_DEVELOPER'">
                    <td style="background: #346100;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'BUILDING_OPERATOR'">
                    <td style="background: #72d200;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'ARCHITECTURE'">
                    <td style="background: #00d8ff;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'FIRE_SAFETY'">
                    <td style="background: #0099b5;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'BUILDING_PHYSICS'">
                    <td style="background: #006577;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'MEP_HVAC'">
                    <td style="background: #0000ff;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'PROCESS_MEASURING_CONTROL'">
                    <td style="background: #43006a;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'BUILDING_CONTRACTOR'">
                    <td style="background: #555555;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:when test="d2p1:NodeManager = 'MANAGER_OF_SUPERIOR_NODE'">
                    <td style="background: #333333;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:when>
                  <xsl:otherwise>
                    <td style="background: #888888;">
                      <xsl:value-of select="d2p1:NodeManager"/>
                    </td>
                  </xsl:otherwise>
                </xsl:choose>
                <td>
                  <xsl:value-of select="d2p1:HasGeometry"/>
                </td>
				<td><xsl:value-of select="count(ancestor::d2p1:Node)"/></td>
		</tr>
		<xsl:apply-templates select="d2p1:ContainedNodes/d2p1:Node"/>
	</xsl:for-each>
  </xsl:template>
  
  
</xsl:stylesheet>
