<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
							  xmlns:n="http://DataContractSerializer.NodeList/"							  
							  xmlns:d2p1="http://DataContractSerializer.Node/">
	<xsl:template match="/">
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
						<th>Name</th>
						<th>Sync</th>
						<th>Unit</th>
						<th>Default Value</th>
						<th>Source</th>
						<th>Manager</th>
						<th>Geometry</th>
						<th>Contained (max. 3 levels)</th>
					</tr>
					<xsl:for-each select="n:Node_List/n:Items/d2p1:Node">
						<tr style="vertical-align:top">
							<td><xsl:value-of select="d2p1:ID"/></td>
              <td>
                <xsl:choose>
                  <xsl:when test="d2p1:SyncByName = 'true'">
                    <span class="mainsynced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                  </xsl:when>
                  <xsl:otherwise>
                    <span class="mainnotsynced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                  </xsl:otherwise>
                </xsl:choose>
              </td>
              <td><xsl:value-of select="d2p1:SyncByName"/></td>
              <td><xsl:value-of select="d2p1:NodeUnit"/></td>							
							<td><xsl:value-of select="d2p1:NodeDefaultVal"/></td>
							<td><xsl:value-of select="d2p1:NodeSource"/></td>
							<xsl:choose>
								<xsl:when test="d2p1:NodeManager = 'REGULATOR'">
									<td style="background: #ff3600;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'MODERATOR'">
									<td style="background: #8e1e00;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'ENERGY_NETWORK_OPERATOR'">
									<td style="background: #a75a00;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'EENERGY_SUPPLIER'">
									<td style="background: #ff8a00;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'BUILDING_DEVELOPER'">
									<td style="background: #346100;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'BUILDING_OPERATOR'">
									<td style="background: #72d200;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'ARCHITECTURE'">
									<td style="background: #00d8ff;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'FIRE_SAFETY'">
									<td style="background: #0099b5;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'BUILDING_PHYSICS'">
									<td style="background: #006577;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'MEP_HVAC'">
									<td style="background: #001c42;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'PROCESS_MEASURING_CONTROL'">
									<td style="background: #43006a;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
                <xsl:when test="d2p1:NodeManager = 'BUILDING_CONTRACTOR'">
									<td style="background: #555555;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:when>
								<xsl:otherwise>
									<td style="background: #999999;"><xsl:value-of select="d2p1:NodeManager"/></td>
								</xsl:otherwise>
							</xsl:choose>
							<td><xsl:value-of select="d2p1:HasGeometry"/></td>
							<td>
								<table class="sortable sortableonecolor">
									<xsl:for-each select="d2p1:ContainedNodes//d2p1:Node[count(ancestor::*) = 4]">										
										<tr style="vertical-align:top">
                      <td>
                        <xsl:choose>
                          <xsl:when test="d2p1:SyncByName = 'true'">
                            <span class="synced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                          </xsl:when>
                          <xsl:otherwise>
                            <span class="notsynced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                          </xsl:otherwise>
                        </xsl:choose>
                      </td>
											<td>
												<span style="font-style:italic;font-size:80%;">
													(<xsl:value-of select="d2p1:NodeDefaultVal"/>, <xsl:value-of select="d2p1:NodeManager"/>)
												</span>
											</td>
											<td>
												<xsl:for-each select="d2p1:ContainedNodes//d2p1:Node[count(ancestor::*) = 6]">
													<tr>
														<td>
                              <xsl:choose>
                                <xsl:when test="d2p1:SyncByName = 'true'">
                                  <span class="synced"><xsl:value-of select="d2p1:NodeName"/></span>                    
                                </xsl:when>
                                <xsl:otherwise>
                                  <span class="notsynced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                                </xsl:otherwise>
                              </xsl:choose>
                            </td>
														<td>
															<span style="font-style:italic;font-size:80%;">
																(<xsl:value-of select="d2p1:NodeDefaultVal"/>, <xsl:value-of select="d2p1:NodeManager"/>)
															</span>
														</td>
														<td>
															<xsl:for-each select="d2p1:ContainedNodes//d2p1:Node[count(ancestor::*) = 8]">
																<tr>
																	<td>
                                    <xsl:choose>
                                      <xsl:when test="d2p1:SyncByName = 'true'">
                                        <span class="synced"><xsl:value-of select="d2p1:NodeName"/></span>                    
                                      </xsl:when>
                                      <xsl:otherwise>
                                        <span class="notsynced"><xsl:value-of select="d2p1:NodeName"/></span>                  
                                      </xsl:otherwise>
                                    </xsl:choose>
                                  </td>
																	<td>
																		<span style="font-style:italic;font-size:80%;">
																			(<xsl:value-of select="d2p1:NodeDefaultVal"/>, <xsl:value-of select="d2p1:NodeManager"/>)
																		</span>
																	</td>
																</tr>
															</xsl:for-each>
														</td>
													</tr>
												</xsl:for-each>
											</td>
										</tr>
									</xsl:for-each>
								</table>
							</td>
						</tr>
					</xsl:for-each>
				</table>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet> 