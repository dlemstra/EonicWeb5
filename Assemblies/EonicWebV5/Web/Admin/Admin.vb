'***********************************************************************
' $Library:     eonic.admin
' $Revision:    3.1  
' $Date:        2006-03-02
' $Author:      Trevor Spink (trevor@eonic.co.uk)
' &Website:     www.eonic.co.uk
' &Licence:     All Rights Reserved.
' $Copyright:   Copyright (c) 2002 - 2006 Eonic Ltd.
'***********************************************************************

Option Strict Off
Option Explicit On

Imports System.Xml
Imports System.Web.HttpUtility
Imports System.Configuration
Imports System.IO
Imports System.Collections
Imports System.Data
Imports System.Data.sqlClient
Imports System.Web.Configuration
Imports System.Text.RegularExpressions
Imports Eonic.Tools
Imports System

Partial Public Class Web
    Public Class Admin

#Region "Declarations"


        Public moPageXML As XmlDocument = New XmlDocument

        Public Shadows mcModuleName As String = "Eonic.Admin"
        Public mcEwCmd As String
        Public mcEwCmd2 As String
        Public mcEwCmd3 As String

        Public moAdXfm As AdminXforms
        'Public moXformEditor As XFormEditor

        Private mcPagePath As String

        'preview mode info
        Public mbPreviewMode As Boolean 'Is Preview mode on?
        Public myWeb As Web
        Public moConfig As System.Collections.Specialized.NameValueCollection

        Public nAdditionId As Integer
        Public moDeniedAdminMenuElmt As XmlElement

        Private mnAdminTopLevel As Integer
        Private lEditContext As String
        Private bClearEditContext As Boolean = True

#End Region
        Sub New()

        End Sub


        Sub New(ByRef aWeb As Web)
            myWeb = aWeb
            moConfig = myWeb.moConfig
            moAdXfm = myWeb.getAdminXform()
            moPageXML = myWeb.moPageXml
            ' moXformEditor = myWeb.GetXformEditor()

            moAdXfm.open(moPageXML)

            If Not myWeb.moSession("EditContext") Is Nothing Then
                EditContext = myWeb.moSession("EditContext")
                clearEditContext = True
            End If

            If CStr(moConfig("AdminRootPageId")) = "" Then
                mnAdminTopLevel = moConfig("RootPageId")
            Else
                mnAdminTopLevel = moConfig("AdminRootPageId")
            End If

        End Sub


        Public Overridable Shadows Sub open(ByVal oPageXml As XmlDocument)
            Dim cProcessInfo As String = ""
            Try

                If mcPagePath = "" Then
                    mcPagePath = "?pgid=" & myWeb.mnPageId & "&"
                End If

            Catch ex As Exception
                returnException(mcModuleName, "open", ex, "", cProcessInfo, gbDebug)
            End Try
        End Sub

        Public Shadows Sub close()
            Dim cProcessInfo As String = ""
            Try

                moAdXfm = Nothing

            Catch ex As Exception
                returnException(mcModuleName, "PersistVariables", ex, "", cProcessInfo, gbDebug)
            End Try
        End Sub

        Public ReadOnly Property Command() As String
            Get
                Return IIf(mcEwCmd Is Nothing OrElse String.IsNullOrEmpty(mcEwCmd), "", mcEwCmd)
            End Get
        End Property

        Public Property EditContext() As String
            'edit context is to define where we are in Content or Mailing List when we are editing content.
            'stored in the session on both the normal and advanced modes which must be visited prior to editing content.
            'principly we use this to define which to adminmenu to show and box styles which might differ between site and emails.
            Get
                Return lEditContext
            End Get
            Set(value As String)
                lEditContext = value
                If value = "" Then
                    clearEditContext = True
                Else
                    clearEditContext = False
                End If
            End Set
        End Property


        Public Property clearEditContext() As Boolean
            'edit context is to define where we are in Content or Mailing List when we are editing content.
            'stored in the session on both the normal and advanced modes which must be visited prior to editing content.
            'principly we use this to define which to adminmenu to show and box styles which might differ between site and emails.
            Get
                Return bClearEditContext
            End Get
            Set(value As Boolean)
                bClearEditContext = value
            End Set
        End Property

        Public Overridable Sub adminProcess(ByRef oWeb As Eonic.Web)
            Dim sAdminLayout As String
            Dim sProcessInfo As String = ""
            Dim oPageDetail As XmlElement = moPageXML.CreateElement("ContentDetail")
            Dim bAdminMode As Boolean = True
            Dim sPageTitle As String = "EonicWeb: "
            Dim bLoadStructure As Boolean = False
            Dim nParId As Long = 0
            Dim bResetParId As Boolean = False
            'get adminxforms ready 
            Dim bMailMenu As Boolean = False
            Dim bSystemPagesMenu As Boolean = False
            Dim nContentId As Long = 0
            Dim clearEditContext As Boolean = True

            moAdXfm.mbAdminMode = myWeb.mbAdminMode

            Try

                oWeb.mbAdminMode = True
                moPageXML = oWeb.moPageXml

                'If myWeb.moSession("previewMode") = "true" Then
                '    mbPreviewMode = True
                '    If IsNumeric(myWeb.moSession("previewUser")) Then oWeb.myWeb.mnUserId = CInt(myWeb.moSession("previewUser"))
                '    If IsDate(myWeb.moSession("previewDate")) Then oWeb.mdDate = CDate(myWeb.moSession("previewDate"))
                'Else
                '    mbPreviewMode = False
                'End If

                Dim EwCmd() As String = Split(myWeb.moRequest("ewCmd"), ".")
                mcEwCmd = EwCmd(0)
                If UBound(EwCmd) > 0 Then mcEwCmd2 = EwCmd(1)
                If UBound(EwCmd) > 1 Then mcEwCmd3 = EwCmd(2)

                If Not moConfig("SecureMembershipAddress") = "" Then
                    Dim oMembership As New Eonic.Web.Membership(myWeb)
                    AddHandler oMembership.OnError, AddressOf myWeb.OnComponentError
                    oMembership.SecureMembershipProcess(mcEwCmd)
                End If

                If mcEwCmd = "" Then
                    mcEwCmd = myWeb.moSession("ewCmd")
                End If

                If myWeb.moRequest("rptCmd") <> "" Then
                    mcEwCmd = "RptCourses"
                End If

                If myWeb.mnUserId > 0 Then
                    'lets check the current users permission level

                    adminAccessRights()

                    If moPageXML.DocumentElement.SelectSingleNode("AdminMenu/MenuItem") Is Nothing Then
                        mcEwCmd = "LogOff"
                    End If

                    'If Not myWeb.moDbHelper.checkUserRole("Administrator") Then
                    '    mcEwCmd = "LogOff"
                    'End If

                ElseIf Not LCase(mcEwCmd) = LCase("LogOff") And Not LCase(mcEwCmd) = LCase("PasswordReminder") And Not LCase(mcEwCmd) = LCase("AR") Then
                    myWeb.moSession("ewAuth") = ""
                    myWeb.mnUserId = 0
                    mcEwCmd = ""
                End If
                'lets remember the page we are editing
                If myWeb.mnPageId < 1 Then
                    myWeb.mnPageId = myWeb.moSession("pgid")
                End If

                sAdminLayout = mcEwCmd

                nParId = CLng("0" & myWeb.moRequest("parId"))
                If nParId = 0 Then
                    nParId = myWeb.moSession("nParId")
                End If

                GoTo ProcessFlow

ProcessFlow:

                Select Case mcEwCmd

                    Case ""
                        If myWeb.mnUserId = 0 Then

                            Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))
                            oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmUserLogon("AdminLogon"))

                            'oPageDetail.AppendChild(moAdXfm.xFrmUserLogon("AdminLogon"))
                            If oMembershipProv.AdminXforms.valid Then

                                adminAccessRights()

                                If moPageXML.DocumentElement.SelectSingleNode("AdminMenu/MenuItem") Is Nothing Then
                                    mcEwCmd = "AdminDenied"
                                End If

                                If mcEwCmd = "AdminDenied" Then
                                    sAdminLayout = "AdminXForm"
                                    myWeb.mnUserId = 0
                                    moAdXfm.addNote(moAdXfm.moXformElmt, xForm.noteTypes.Alert, "You do not have administrative access to this site.")
                                Else
                                    Dim oAdminRoot As XmlElement = myWeb.moPageXml.SelectSingleNode("/Page/AdminMenu/MenuItem")
                                    mcEwCmd = oAdminRoot.GetAttribute("cmd")
                                    If mcEwCmd = "" Then mcEwCmd = "Normal"

                                    If mcEwCmd = "Normal" Then
                                        sAdminLayout = ""
                                    Else
                                        sAdminLayout = mcEwCmd
                                    End If

                                    oPageDetail.RemoveAll()

                                    myWeb.moSession("adminMode") = "true"

                                    If gbSingleLoginSessionPerUser Then
                                        myWeb.moDbHelper.logActivity(dbHelper.ActivityType.Logon, myWeb.mnUserId, 0)
                                    End If

                                    GoTo ProcessFlow

                                    ''Get Status for Dashboard...
                                    'Dim statusElmt As XmlElement = moPageXML.CreateElement("Status")
                                    'statusElmt.InnerXml = myWeb.GetStatus().OuterXml
                                    'oPageDetail.AppendChild(statusElmt)

                                End If
                            Else
                                sAdminLayout = "AdminXForm"
                            End If
                        Else
                            If myWeb.mnPageId > 0 Then
                                If myWeb.moSession("ewCmd") = "" Then
                                    mcEwCmd = "Normal"
                                Else
                                    mcEwCmd = myWeb.moSession("ewCmd")
                                End If
                            End If
                        End If

                    Case "PasswordReminder"
                        sAdminLayout = "AdminXForm"
                        Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))

                        Select Case LCase(moConfig("MembershipEncryption"))
                            Case "md5salt", "md5", "sha1", "sha256"
                                oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmResetAccount())
                            Case Else
                                oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmPasswordReminder())
                        End Select
                    Case "AR"
                        sAdminLayout = "AdminXForm"
                        Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))
                        oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmConfirmPassword(myWeb.moRequest("AI")))
                        If oMembershipProv.AdminXforms.valid = True Then

                            adminAccessRights()

                            Dim oAdminRoot As XmlElement = myWeb.moPageXml.SelectSingleNode("/Page/AdminMenu/MenuItem")
                            mcEwCmd = oAdminRoot.GetAttribute("cmd")
                            If mcEwCmd = "" Then mcEwCmd = "Normal"

                            If mcEwCmd = "Normal" Then
                                sAdminLayout = ""
                            Else
                                sAdminLayout = mcEwCmd
                            End If
                            oPageDetail.RemoveAll()

                            myWeb.msRedirectOnEnd = "/"

                            GoTo ProcessFlow
                        End If
                    Case "LogOff"

                        If gbSingleLoginSessionPerUser Then
                            myWeb.moDbHelper.logActivity(dbHelper.ActivityType.Logoff, myWeb.mnUserId, 0)
                            If myWeb.moRequest.Cookies("ewslock") IsNot Nothing Then
                                myWeb.moResponse.Cookies("ewslock").Expires = DateTime.Now.AddDays(-1)
                            End If
                        End If
                        myWeb.moSession("ewAuth") = ""
                        myWeb.moSession("nUserId") = 0
                        myWeb.moSession.Abandon()
                        myWeb.mnUserId = 0
                        myWeb.msRedirectOnEnd = moConfig("ProjectPath") & "/" & mcPagePath

                    Case "AdmHome", "Admin"
                        Dim statusElmt As XmlElement = moPageXML.CreateElement("Status")
                        statusElmt.InnerXml = myWeb.GetStatus().OuterXml
                        oPageDetail.AppendChild(statusElmt)

                    Case ("MemberActivity")
                        MemberActivityProcess(oPageDetail, sAdminLayout)

                    Case "MemberCodes"
                        MemberCodesProcess(oPageDetail, sAdminLayout)

                    Case "Sync"

                        myWeb.oSync = New ExternalSynchronisation(myWeb, oPageDetail)
                        'AddHandler oSync.OnError, AddressOf OnError
                        sAdminLayout = myWeb.oSync.AdminProcess(myWeb.moRequest("ewCmd2"))
                        bLoadStructure = True
                    Case "FileImport"
                        FileImportProcess(oPageDetail, sAdminLayout)

                    Case "AwaitingApproval"
                        VersionControlProcess(oPageDetail, sAdminLayout)

                    Case "admin"
                        mcEwCmd = "Content"
                        GoTo ProcessFlow


                    Case "adminDenied"

                        sAdminLayout = "adminDenied"

                        oPageDetail.AppendChild(moDeniedAdminMenuElmt)

                    Case "WebSettings"

                        If mcEwCmd2 = Nothing Then
                            oPageDetail.AppendChild(moAdXfm.xFrmWebConfig("General"))
                        Else
                            oPageDetail.AppendChild(moAdXfm.xFrmWebConfig(mcEwCmd2))
                        End If
                        If moAdXfm.valid Then
                            mcEwCmd = "Normal"
                            myWeb.moCtx.Application("ewSettings") = Nothing
                            myWeb.msRedirectOnEnd = "/?ewCmd=SettingsDash"
                            myWeb.ClearPageCache()

                        Else
                            sAdminLayout = "AdminXForm"
                        End If
                    Case "301Redirect", "302Redirect", "StaticRewrites"

                        oPageDetail.AppendChild(moAdXfm.xFrmRewriteMaps(mcEwCmd))

                        sAdminLayout = "AdminXForm"

                    Case "RewriteRules"

                        Dim oCfg As Configuration = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/")

                        Dim oImp As Eonic.Tools.Security.Impersonate = New Eonic.Tools.Security.Impersonate
                        If oImp.ImpersonateValidUser(moConfig("AdminAcct"), moConfig("AdminDomain"), moConfig("AdminPassword"), , moConfig("AdminGroup")) Then

                            'code here to replace any missing nodes
                            'all of the required config settings

                            Dim rewriteXml As New XmlDocument
                            rewriteXml.Load(myWeb.goServer.MapPath("/RewriteRules.config"))

                            Dim defaultXml As New XmlDocument
                            defaultXml.Load(myWeb.goServer.MapPath("/ewcommon/setup/rootfiles/RewriteRules_config.xml"))
                            Dim oRule As XmlElement
                            For Each oRule In rewriteXml.DocumentElement.SelectNodes("rule")
                                Dim rulename As String = oRule.GetAttribute("name")
                                Try
                                    Dim defaultRule As XmlElement = defaultXml.SelectSingleNode("descendant-or-self::rule[@name=" & xPathEscapeQuote(rulename) & "]")
                                    If defaultRule Is Nothing Then
                                        oRule.SetAttribute("matchDefault", "create")
                                    Else
                                        If oRule.OuterXml = defaultRule.OuterXml Then
                                            oRule.SetAttribute("matchDefault", "true")
                                        Else
                                            oRule.SetAttribute("matchDefault", "reset")
                                        End If
                                    End If
                                Catch ex As Exception
                                    oRule.SetAttribute("matchDefault", ex.Message)
                                End Try




                            Next
                            Dim rulesElmt As XmlElement = moPageXML.CreateElement("RewriteRules")
                            rulesElmt.InnerXml = rewriteXml.DocumentElement.OuterXml
                            oPageDetail.AppendChild(rulesElmt)

                        End If

                        sAdminLayout = "RewriteRules"

                    Case "SelectTheme"

                        oPageDetail.AppendChild(moAdXfm.xFrmSelectTheme())
                        If moAdXfm.valid Then
                            mcEwCmd = "Normal"
                            myWeb.msRedirectOnEnd = "/?ewCmd=ThemeSettings"
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "InstallTheme"

                        Dim SoapObj As New eonicweb.com.ewAdminProxySoapClient
                        Dim themesPage As System.Xml.Linq.XElement = SoapObj.GetThemes(System.Environment.MachineName, myWeb.moRequest.ServerVariables("SERVER_NAME"))
                        Dim ContextThemesPage As XmlElement = moPageXML.CreateElement("Themes")
                        Dim xreader As XmlReader = themesPage.CreateReader()
                        xreader.MoveToContent()
                        ContextThemesPage.InnerXml = xreader.ReadInnerXml()
                        Dim oTheme As XmlElement
                        For Each oTheme In ContextThemesPage.SelectNodes("descendant-or-self::Content[@type='EwTheme']")
                            oPageDetail.AppendChild(oTheme)
                        Next
                        Dim themeName As String = myWeb.moRequest("themeName")
                        If themeName <> "" Then
                            Dim fileBytes As Byte()
                            fileBytes = SoapObj.GetThemeZip(System.Environment.MachineName, myWeb.moRequest.ServerVariables("SERVER_NAME"), themeName)

                            If Not fileBytes Is Nothing Then

                                Dim strdocPath As String
                                strdocPath = myWeb.goServer.MapPath(moConfig("ProjectPath") & "/ewThemes/" & themeName & ".zip")
                                Dim objfilestream As New FileStream(strdocPath, FileMode.Create, FileAccess.ReadWrite)
                                objfilestream.Write(fileBytes, 0, fileBytes.Length)
                                objfilestream.Close()

                                'unzip the transfered file
                                Dim fz As New ICSharpCode.SharpZipLib.Zip.FastZip
                                fz.ExtractZip(myWeb.goServer.MapPath(moConfig("ProjectPath") & "/ewThemes/" & themeName & ".zip"), myWeb.goServer.MapPath(moConfig("ProjectPath") & "/ewThemes/"), "")

                                'delete the transfered file
                                Dim oFile As New System.IO.FileInfo(myWeb.goServer.MapPath(moConfig("ProjectPath") & "/ewThemes/" & themeName & ".zip"))
                                oFile.Delete()
                                mcEwCmd = "SelectTheme"
                                GoTo ProcessFlow
                            Else
                                sAdminLayout = "InstallTheme"
                                oPageDetail.SetAttribute("errorMsg", "Theme Installation Failed")

                            End If

                        Else
                            sAdminLayout = "InstallTheme"
                        End If

                    Case "ThemeSettings"

                        Dim moThemeConfig As System.Collections.Specialized.NameValueCollection = WebConfigurationManager.GetWebApplicationSection("eonic/theme")

                        oPageDetail.AppendChild(moAdXfm.xFrmThemeSettings("../../ewThemes/" & moThemeConfig("CurrentTheme") & "/xforms/Config/SkinSettings"))

                        If moAdXfm.valid Then

                            updateLessVariables(moThemeConfig("CurrentTheme"), moAdXfm.Instance)
                            updateStandardXslVariables(moThemeConfig("CurrentTheme"), moAdXfm.Instance)

                            If myWeb.moRequest("SiteXsl") <> "" Then
                                Eonic.Config.UpdateConfigValue(myWeb, "eonic/web", "SiteXsl", myWeb.moRequest("SiteXsl"))
                            End If

                            myWeb.moCtx.Application("ewSettings") = Nothing
                            mcEwCmd = "Normal"
                            myWeb.msRedirectOnEnd = "/?rebundle=true"

                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "Normal", "ByPage", "Content"
                        mcEwCmd = "Normal"
                        sAdminLayout = ""
                        EditContext = "Normal"

                        If Not myWeb.mbPopupMode Then
                            If myWeb.moRequest("pgid") <> "" Then
                                'lets save the page we are editing to the session
                                myWeb.moSession("pgid") = myWeb.moRequest("pgid")
                                If Not myWeb.mbSuppressLastPageOverrides Then
                                    myWeb.moSession("lastPage") = myWeb.mcPagePath & "/?ewCmd=Normal&pgid=" & myWeb.mnPageId 'myWeb.mcOriginalURL
                                End If
                            End If
                            'we want to return here after editing
                            If Not myWeb.mbSuppressLastPageOverrides Then
                                myWeb.moSession("lastPage") = myWeb.mcPagePath & "/?ewCmd=Normal&pgid=" & myWeb.mnPageId '
                            End If

                        End If

                    Case "Advanced"
                        sAdminLayout = "Advanced"
                        EditContext = "Advanced"

                        Dim oCommonContentTypes As New XmlDocument
                        If IO.File.Exists(myWeb.goServer.MapPath("/ewcommon/xsl/pagelayouts/layoutmanifest.xml")) Then oCommonContentTypes.Load(myWeb.goServer.MapPath("/ewcommon/xsl/pagelayouts/layoutmanifest.xml"))
                        If IO.File.Exists(myWeb.goServer.MapPath(moConfig("ProjectPath") & "/xsl/layoutmanifest.xml")) Then
                            Dim oLocalContentTypes As New XmlDocument
                            oLocalContentTypes.Load(myWeb.goServer.MapPath(moConfig("ProjectPath") & "/xsl/layoutmanifest.xml"))
                            Dim oLocals As XmlElement = oLocalContentTypes.SelectSingleNode("/PageLayouts/ContentTypes")
                            If Not oLocals Is Nothing Then
                                Dim oGrp As XmlElement
                                For Each oGrp In oLocals.SelectNodes("ContentTypeGroup")
                                    Dim oComGrp As XmlElement = oCommonContentTypes.SelectSingleNode("/PageLayouts/ContentTypes/ContentTypeGroup[@name='" & oGrp.GetAttribute("name") & "']")
                                    If Not oComGrp Is Nothing Then
                                        Dim oTypeElmt As XmlElement
                                        For Each oTypeElmt In oGrp.SelectNodes("ContentType")
                                            If Not oComGrp.SelectSingleNode("ContentType[@type='" & oTypeElmt.GetAttribute("type") & "']") Is Nothing Then
                                                oComGrp.SelectSingleNode("ContentType[@type='" & oTypeElmt.GetAttribute("type") & "']").InnerText = oTypeElmt.InnerText
                                            Else
                                                oComGrp.InnerXml &= oTypeElmt.OuterXml
                                            End If
                                        Next
                                    Else
                                        oCommonContentTypes.DocumentElement.SelectSingleNode("ContentTypes").InnerXml &= oGrp.OuterXml
                                    End If
                                Next
                            End If
                        End If
                        'now to add it to the pagexml
                        oPageDetail.AppendChild(moPageXML.ImportNode(oCommonContentTypes.SelectSingleNode("/PageLayouts/ContentTypes"), True))


                        If myWeb.moRequest("pgid") <> "" Then
                            'lets save the page we are editing to the session
                            myWeb.moSession("pgid") = myWeb.moRequest("pgid")
                        End If
                        'we want to return here after editing
                        myWeb.moSession("lastPage") = myWeb.mcOriginalURL

                    Case "ByType"
                        'list all content of a specific type

                        sAdminLayout = "ByType"
                        EditContext = "ByType." & mcEwCmd2
                        myWeb.moSession("lastPage") = "?ewCmd=ByType." & mcEwCmd2 & "." & mcEwCmd3
                        Dim ContentType As String = mcEwCmd2
                        Dim Filter As String = mcEwCmd3
                        Dim FilterSQL As String = ""
                        Dim FilterArr As String()

                        If Filter <> "" Then
                            FilterArr = Filter.Split(":")
                            Dim FilterName As String = FilterArr(0)
                            Dim FilterValue As String
                            If FilterArr.Length = 1 Then
                                FilterValue = myWeb.moRequest(FilterName)
                            Else
                                FilterValue = FilterArr(1)
                            End If
                            Select Case FilterArr(0)
                                Case "Location"
                                    'Get a list of possible locations for this content type
                                    Dim oXfrm As New xForm(myWeb)
                                    oXfrm.NewFrm("LocationFilter")
                                    oXfrm.submission("LocationFilter", "/?ewCmd=ByType." & ContentType & ".Location", "post", "")
                                    Dim sSql As String = "select dbo.fxn_getPagePath(nStructKey) as name, nStructKey as value from tblContentStructure where nStructKey in " & _
"(select nStructId from tblContentLocation cl inner join tblContent c on cl.nContentID = c.nContentKey where cContentSchemaName = '" & ContentType & "' and bPrimary = 1 )"
                                    Dim locSelect As XmlElement = oXfrm.addSelect1(oXfrm.moXformElmt, "Location", False, "Select Location", "submit-on-select")

                                    If myWeb.moRequest("Location") <> "" Then
                                        FilterValue = myWeb.moRequest("Location")
                                        myWeb.mnPageId = CLng("0" & FilterValue)
                                    Else
                                        If myWeb.moSession("FilterValue") <> "" Then
                                            FilterValue = myWeb.moSession("FilterValue")
                                        End If
                                    End If

                                    oXfrm.addValue(locSelect, FilterValue)

                                    oXfrm.addUserOptionsFromSqlDataReader(locSelect, myWeb.moDbHelper.getDataReader(sSql))
                                    oPageDetail.AppendChild(oXfrm.moXformElmt)
                                    myWeb.ClearPageCache()

                                    'get a list of pages with this content on.
                                    If FilterValue <> "" Then
                                        FilterSQL = " CL.nStructId = '" & FilterValue & "'"
                                        myWeb.GetContentXMLByType(moPageXML.DocumentElement, ContentType & "|ASC_cl.nDisplayOrder", FilterSQL)
                                        myWeb.moSession("FilterValue") = FilterValue
                                    End If

                                Case "User"
                                    If myWeb.moDbHelper.checkUserRole("Administrator") Then
                                        myWeb.GetContentXMLByType(moPageXML.DocumentElement, ContentType)
                                    Else
                                        FilterSQL = " a.nInsertDirId = '" & myWeb.mnUserId & "'"
                                        myWeb.GetContentXMLByType(moPageXML.DocumentElement, ContentType, FilterSQL)
                                    End If
                                Case "Sale"
                                    myWeb.GetContentXMLByType(moPageXML.DocumentElement, ContentType)


                            End Select
                        Else

                            myWeb.GetContentXMLByType(moPageXML.DocumentElement, ContentType)

                        End If



                    Case "AddModule"

                        bLoadStructure = True
                        nAdditionId = 0

                        oPageDetail.AppendChild(moAdXfm.xFrmAddModule(myWeb.moRequest("pgid"), myWeb.moRequest("position")))
                        If moAdXfm.valid Then
                            myWeb.ClearPageCache()

                            If myWeb.moRequest("nStatus") <> "" Then
                                oPageDetail.RemoveAll()
                                If myWeb.moSession("lastPage") <> "" Then
                                    myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                    myWeb.moSession("lastPage") = ""
                                Else
                                    mcEwCmd = "Normal"
                                    oPageDetail.RemoveAll()
                                    moAdXfm.valid = False
                                    GoTo ProcessFlow
                                End If
                            Else
                                mcEwCmd = "AddContent"
                                sAdminLayout = "AdminXForm"
                            End If

                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "AddContent"

                        bLoadStructure = True
                        nAdditionId = 0
                        nContentId = 0
                        bClearEditContext = False

                        oPageDetail.AppendChild(moAdXfm.xFrmEditContent(0, myWeb.moRequest("type"), CLng(myWeb.moRequest("pgid")), myWeb.moRequest("name"), , nAdditionId))
                        If moAdXfm.valid Then
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            myWeb.ClearPageCache()

                            'if we have a parent releationship lets add it but not if we have a relation type becuase that happens in the xform.
                            If myWeb.moRequest("contentParId") <> "" AndAlso IsNumeric(myWeb.moRequest("contentParId")) Then
                                Dim b2Way As Boolean = IIf(myWeb.moRequest("RelType") = "2way" Or myWeb.moRequest("direction") = "2Way", True, False)
                                Dim sRelType As String = myWeb.moRequest("relationType")
                                myWeb.moDbHelper.insertContentRelation(myWeb.moRequest("contentParId"), nAdditionId, b2Way, sRelType)
                            End If

                            oPageDetail.RemoveAll()

                            If myWeb.moSession("lastPage") <> "" Then
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                myWeb.moSession("lastPage") = ""
                            Else
                                oPageDetail.RemoveAll()
                                moAdXfm.valid = False
                                GoTo ProcessFlow
                            End If

                        Else
                            sAdminLayout = "AdminXForm"
                        End If
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow

                    Case "EditContent"
                        ' Get a version Id if it's passed through.
                        Dim cVersionKey As String = myWeb.moRequest("verId") & ""
                        bClearEditContext = False
                        bLoadStructure = True
                        If Not (IsNumeric(cVersionKey)) Then cVersionKey = "0"
                        nContentId = 0
                        oPageDetail.AppendChild(moAdXfm.xFrmEditContent(myWeb.moRequest("id"), "", CLng(myWeb.moRequest("pgid")), , , nContentId, , , CLng(cVersionKey)))

                        If moAdXfm.valid Then
                            bAdminMode = False
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")

                            myWeb.ClearPageCache()

                            'if we have a parent releationship lets add it
                            If myWeb.moRequest("contentParId") <> "" AndAlso IsNumeric(myWeb.moRequest("contentParId")) Then
                                myWeb.moDbHelper.insertContentRelation(myWeb.moRequest("contentParId"), nContentId)
                            End If
                            If myWeb.moRequest("EditXForm") <> "" Then
                                bAdminMode = True
                                sAdminLayout = "AdminXForm"
                                mcEwCmd = "EditXForm"
                                oPageDetail = oWeb.GetContentDetailXml(, myWeb.moRequest("id"))
                            Else
                                myWeb.mnArtId = 0
                                oPageDetail.RemoveAll()

                                ' Check for an optional command to redireect to
                                If Not (String.IsNullOrEmpty("" & myWeb.moRequest("ewRedirCmd"))) Then

                                    myWeb.msRedirectOnEnd = moConfig("ProjectPath") & "/?ewCmd=" & myWeb.moRequest("ewRedirCmd")

                                ElseIf myWeb.moSession("lastPage") <> "" Then
                                    myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                    myWeb.moSession("lastPage") = ""
                                Else
                                    oPageDetail.RemoveAll()
                                    moAdXfm.valid = False
                                    GoTo ProcessFlow
                                End If
                            End If
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "RollbackContent"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmEditContent(myWeb.moRequest("id"), "", CLng(myWeb.moRequest("pgid")), , , , , , myWeb.moRequest("verId")))
                        If moAdXfm.valid Then
                            bAdminMode = False
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            myWeb.mnArtId = 0
                            oPageDetail.RemoveAll()
                            myWeb.ClearPageCache()

                            'lest just try this redirecting to last page
                            If mcEwCmd = "Normal" Then
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                            End If
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "CopyContent"
                        bLoadStructure = True
                        bClearEditContext = False
                        oPageDetail.AppendChild(moAdXfm.xFrmEditContent(myWeb.moRequest("id"), "", CLng(myWeb.moRequest("pgid")), , True))
                        If moAdXfm.valid Then
                            bAdminMode = False
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            If myWeb.moRequest("submit") = "Edit Questions" Then
                                bAdminMode = True
                                sAdminLayout = "AdminXForm"
                                mcEwCmd = "EditXForm"
                                oPageDetail = oWeb.GetContentDetailXml(, myWeb.moRequest("id"))
                            Else
                                myWeb.mnArtId = 0
                                oPageDetail.RemoveAll()
                            End If
                            myWeb.ClearPageCache()

                            GoTo processflow
                        Else
                            sAdminLayout = "AdminXForm"
                        End If


                    Case "UpdateContentValue"

                        Dim nMyContentId As String = myWeb.moRequest("id")
                        Dim cContentXpath As String = myWeb.moRequest("xpath")
                        Dim cContentValue As String = myWeb.moRequest("value")
                        Dim oTempInstance As New XmlDocument

                        If Not cContentXpath.StartsWith("/instance/tblContent/cContentXmlBrief/Content") Then
                            cContentXpath = "/instance/tblContent/cContentXmlBrief/Content" & cContentXpath
                        End If

                        oTempInstance.InnerXml = "<instance>" & myWeb.moDbHelper.getObjectInstance(dbHelper.objectTypes.Content, nMyContentId) & "</instance>"
                        Dim ElmtToChange As XmlElement = oTempInstance.DocumentElement.SelectSingleNode(cContentXpath)
                        If Not ElmtToChange Is Nothing Then
                            ElmtToChange.InnerText = cContentValue
                        End If
                        ElmtToChange = oTempInstance.DocumentElement.SelectSingleNode(cContentXpath.Replace("cContentXmlBrief", "cContentXmlDetail"))
                        If Not ElmtToChange Is Nothing Then
                            ElmtToChange.InnerText = cContentValue
                        End If
                        myWeb.moDbHelper.setObjectInstance(dbHelper.objectTypes.Content, oTempInstance.DocumentElement, nMyContentId)
                        myWeb.ClearPageCache()

                        sAdminLayout = "AjaxReturnTrue"

                    Case "HideContent"
                        ' hide content
                        myWeb.moDbHelper.setObjectStatus(dbHelper.objectTypes.Content, dbHelper.Status.Hidden, myWeb.moRequest("id"))

                        mcEwCmd = myWeb.moSession("ewCmd")
                        'lest just try this redirecting to last page

                        If myWeb.moSession("lastPage") <> "" Then
                            myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                            myWeb.moSession("lastPage") = ""
                        Else
                            oPageDetail.RemoveAll()
                            moAdXfm.valid = False
                            GoTo ProcessFlow
                        End If
                        myWeb.ClearPageCache()

                        oPageDetail.RemoveAll()
                        myWeb.mnArtId = 0
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow

                    Case "RemoveContentRelation"
                        ' remove content relation from item
                        myWeb.moDbHelper.RemoveContentRelation(myWeb.moRequest("relId"), myWeb.moRequest("id"))
                        mcEwCmd = myWeb.moSession("ewCmd")
                        'lest just try this redirecting to last page
                        If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                            myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                        End If
                        myWeb.ClearPageCache()
                        oPageDetail.RemoveAll()
                        myWeb.mnArtId = 0
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow

                    Case "RemoveContentLocation"
                        ' remove content relation from item
                        myWeb.moDbHelper.RemoveContentLocation(myWeb.moRequest("pgid"), myWeb.moRequest("id"))
                        mcEwCmd = myWeb.moSession("ewCmd")
                        'lest just try this redirecting to last page
                        If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                            myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                        End If
                        myWeb.ClearPageCache()
                        oPageDetail.RemoveAll()
                        myWeb.mnArtId = 0
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow

                    Case "ShowContent"
                        ' hide content
                        myWeb.moDbHelper.setObjectStatus(dbHelper.objectTypes.Content, dbHelper.Status.Live, myWeb.moRequest("id"))
                        mcEwCmd = myWeb.moSession("ewCmd")
                        'lest just try this redirecting to last page
                        If myWeb.moSession("lastPage") <> "" Then
                            myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                            myWeb.moSession("lastPage") = ""
                        Else
                            oPageDetail.RemoveAll()
                            moAdXfm.valid = False
                            GoTo ProcessFlow
                        End If
                        myWeb.ClearPageCache()
                        oPageDetail.RemoveAll()
                        myWeb.mnArtId = 0
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow

                    Case "DeleteContent"
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteContent(myWeb.moRequest("id")))
                        If moAdXfm.valid Then
                            bAdminMode = False
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            'lest just try this redirecting to last page
                            myWeb.ClearPageCache()
                            If myWeb.moSession("lastPage") <> "" Then
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                myWeb.moSession("lastPage") = ""
                            Else
                                oPageDetail.RemoveAll()
                                moAdXfm.valid = False
                                GoTo ProcessFlow
                            End If
                        Else
                            sAdminLayout = "AdminXForm"
                        End If
                        If mcEwCmd = "Advanced" Then GoTo ProcessFlow
                    Case "UpdatePosition"
                        If myWeb.moRequest("id") <> "" Then
                            Dim reorder As Boolean = IIf(myWeb.moRequest("reorder") = "false", False, True)
                            myWeb.moDbHelper.updatePagePosition(myWeb.moRequest("pgid"), myWeb.moRequest("id"), myWeb.moRequest("position"))
                            'oPageDetail.RemoveAll()
                            'output nothing ,just called by AJAX
                            myWeb.ClearPageCache()
                        End If
                    Case "MoveContent"
                        If myWeb.moRequest("parId") <> "" Then
                            myWeb.moDbHelper.moveContent(CLng(myWeb.moRequest("id")), CLng(myWeb.moRequest("pgid")), CLng(myWeb.moRequest("parId")))
                            'lets show the target page content
                            myWeb.mnPageId = CLng(myWeb.moRequest("parId"))
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            'lest just try this redirecting to page we moved it to
                            If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                                myWeb.msRedirectOnEnd = "?ewCmd=" & mcEwCmd & "&pgid=" & myWeb.mnPageId 'myWeb.moSession("lastPage")
                            End If
                            oPageDetail.RemoveAll()
                            myWeb.ClearPageCache()
                        End If
                        bLoadStructure = True
                        bMailMenu = True
                        'Xform Stuff

                    Case "LocateContent"
                        If myWeb.moRequest("submit") <> "" Then
                            'updateLocations
                            myWeb.moDbHelper.updateLocations(myWeb.moRequest("id"), myWeb.moRequest("location"), myWeb.moRequest("position"))
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            'lest just try this redirecting to last page
                            If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                                myWeb.msRedirectOnEnd = "?ewCmd=" & mcEwCmd & "&pgid=" & myWeb.mnPageId 'myWeb.moSession("lastPage")
                            End If
                            bAdminMode = False
                            oPageDetail.RemoveAll()
                            myWeb.mnArtId = 0
                            myWeb.ClearPageCache()
                            If mcEwCmd <> "Normal" Then
                                GoTo ProcessFlow
                            End If

                        Else
                            bLoadStructure = True
                            Dim oContentXml As XmlElement
                            oContentXml = myWeb.GetContentBriefXml(, myWeb.moRequest("id")).SelectSingleNode("/ContentDetail/Content")
                            myWeb.moContentDetail = myWeb.moDbHelper.getLocationsByContentId(myWeb.moRequest("id"), oContentXml)

                            bMailMenu = True
                            bSystemPagesMenu = True

                        End If
                    Case "LocateContentDetail"
                        oPageDetail.AppendChild(moAdXfm.xFrmContentLocationDetail(myWeb.moRequest("pgid"), myWeb.moRequest("id")))
                        sAdminLayout = "AdminXForm"
                        bMailMenu = True
                        bSystemPagesMenu = True
                        If moAdXfm.valid Then
                            sAdminLayout = ""
                            mcEwCmd = myWeb.moSession("ewCmd")
                            bAdminMode = False
                            oPageDetail.RemoveAll()
                            myWeb.mnArtId = 0
                            myWeb.ClearPageCache()
                        End If
                    Case "ContentVersions"
                        sAdminLayout = "ContentVersions"
                        oPageDetail.AppendChild(myWeb.moDbHelper.getContentVersions(myWeb.moRequest("id")))


                        'Menu Stuff
                    Case "MoveHere"
                        bLoadStructure = True
                        myWeb.moDbHelper.moveStructure(CLng(myWeb.moRequest("pgid")), CLng(myWeb.moRequest("parid")))
                        sAdminLayout = "EditStructure"
                        mcEwCmd = "MovePage"
                        myWeb.ClearPageCache()


                    Case "ContentLocations", "MovePage"
                        bLoadStructure = True

                    Case "PageVersions"

                        sAdminLayout = "PageVersions"
                        oPageDetail.AppendChild(myWeb.moDbHelper.getPageVersions(myWeb.moRequest("pgid")))

                    Case "NewPageVersion"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmCopyPageVersion(myWeb.moRequest("pgid"), myWeb.moRequest("vParId")))
                        If moAdXfm.valid Then
                            mcEwCmd = myWeb.moSession("ewCmd")
                            sAdminLayout = ""
                            oPageDetail.RemoveAll()
                            myWeb.ClearPageCache()
                        Else
                            sAdminLayout = "PageSettings"
                        End If

                    Case "PageVersionMoveUp", "PageVersionMoveDown"
                        'we are sorting the menu nodes
                        Dim direction As String = ""
                        Select Case mcEwCmd
                            Case "PageVersionMoveUp"
                                direction = "MoveUp"
                            Case "PageVersionMoveDown"
                                direction = "MoveDown"
                        End Select
                        myWeb.moDbHelper.ReorderNode(dbHelper.objectTypes.PageVersion, myWeb.moRequest("pgid"), direction)
                        mcEwCmd = "PageVersions"
                        myWeb.ClearPageCache()
                        GoTo ProcessFlow
                    Case "CopyPage"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmCopyPage(myWeb.moRequest("pgid")))
                        If moAdXfm.valid Then
                            mcEwCmd = myWeb.moSession("ewCmd")
                            sAdminLayout = ""
                            oPageDetail.RemoveAll()
                            Dim cUrl As String = myWeb.moConfig("ProjectPath") & "/?ewCmd=" & mcEwCmd
                            myWeb.msRedirectOnEnd = cUrl
                            myWeb.ClearPageCache()
                        Else
                            sAdminLayout = "PageSettings"
                        End If
                    Case "EditPage", "AddPage"
                        bLoadStructure = True

                        'TS thinking about how to manage redirects.
                        'We have a problem when going through edit menu then editing metatags you return to structure or normal.

                        'Dim returnCmd As String = CStr(myWeb.moRequest("returnCmd") & "")
                        'If Not myWeb.moSession("lastPage") Is Nothing Then
                        '    If returnCmd <> "" And Not (myWeb.moSession("lastPage").contains(returnCmd)) Then
                        '        myWeb.moSession("lastPage") = ""
                        '    End If
                        'End If

                        oPageDetail.AppendChild(moAdXfm.xFrmEditPage(myWeb.moRequest("pgid"), myWeb.moRequest("name")))
                        If moAdXfm.valid Then
                            sAdminLayout = ""
                            'oPageDetail.RemoveAll()
                            If myWeb.moRequest("BehaviourAddPageCommand") <> "" Then myWeb.mcBehaviourAddPageCommand = myWeb.moRequest("BehaviourAddPageCommand")
                            If myWeb.moRequest("BehaviourEditPageCommand") <> "" Then myWeb.mcBehaviourEditPageCommand = myWeb.moRequest("BehaviourEditPageCommand")

                            ' Trev's change circa 30 Oct 2009 to set a nominal behaviour for page settings (appears to be return to Page Settings page whatever)
                            ' If either addpage or editpage behaviours have been set then we'll do something else.
                            If String.IsNullOrEmpty(myWeb.mcBehaviourAddPageCommand) And String.IsNullOrEmpty(myWeb.mcBehaviourEditPageCommand) Then

                                ' Default behaviour
                                If myWeb.moSession("lastPage") <> "" Then
                                    myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                    myWeb.moSession("lastPage") = ""
                                Else
                                    ' Edit page - just do the normal stuff
                                    mcEwCmd = myWeb.moSession("ewCmd")
                                    myWeb.mnPageId = myWeb.moSession("pgid")
                                    oPageDetail.RemoveAll()
                                End If

                            Else

                                ' Bespoke behaviour - is it edit or add
                                If myWeb.moRequest("pgid") > 0 Then
                                    mcEwCmd = myWeb.mcBehaviourEditPageCommand
                                Else
                                    mcEwCmd = myWeb.mcBehaviourAddPageCommand
                                End If
                                myWeb.mnPageId = Tools.Xml.getNodeValueByType(moAdXfm.Instance, "//nStructKey", Tools.Xml.XmlDataType.TypeNumber, CLng(myWeb.moSession("pgid")))

                                ' Check if this page is a cloned page
                                Dim nCloneId As Long = Tools.Xml.getNodeValueByType(moAdXfm.Instance, "//nCloneStructId", Tools.Xml.XmlDataType.TypeNumber, 0)

                                ' Force a redirect

                                myWeb.moSession("ewCmd") = mcEwCmd
                                myWeb.moSession("pgid") = myWeb.mnPageId

                                Dim cUrl As String = myWeb.moConfig("ProjectPath") & "/?ewCmd=" & mcEwCmd & "&pgid=" & myWeb.mnPageId
                                If nCloneId > 0 Then cUrl &= "&context=" & myWeb.mnPageId
                                myWeb.msRedirectOnEnd = cUrl

                            End If
                            myWeb.ClearPageCache()
                        Else
                            'come back here if not going back elsewhere such as EditStucture

                            If myWeb.moSession("lastPage") = "" Then
                                myWeb.moSession("lastPage") = myWeb.mcOriginalURL
                            End If
                            sAdminLayout = "PageSettings"
                        End If
                    Case "EditPageLayout"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmEditPageLayout(myWeb.moRequest("pgid")))
                        If moAdXfm.valid Then
                            mcEwCmd = myWeb.moSession("ewCmd")
                            sAdminLayout = ""
                            oPageDetail.RemoveAll()
                            myWeb.ClearPageCache()
                        Else
                            sAdminLayout = "AdminXForm"
                        End If
                    Case "HidePage"
                        ' hide content
                        myWeb.moDbHelper.setObjectStatus(dbHelper.objectTypes.ContentStructure, dbHelper.Status.Hidden, myWeb.moRequest("pgid"))
                        bLoadStructure = True
                        mcEwCmd = myWeb.moSession("ewCmd")
                        sAdminLayout = ""
                        oPageDetail.RemoveAll()
                        'Redirect to previous page
                        myWeb.mnPageId = myWeb.moSession("pgid")
                        myWeb.ClearPageCache()
                    Case "ShowPage"
                        ' Show Page
                        myWeb.moDbHelper.setObjectStatus(dbHelper.objectTypes.ContentStructure, dbHelper.Status.Live, myWeb.moRequest("pgid"))
                        bLoadStructure = True
                        mcEwCmd = myWeb.moSession("ewCmd")
                        sAdminLayout = ""
                        oPageDetail.RemoveAll()
                        myWeb.ClearPageCache()
                        'Redirect to previous page
                        myWeb.mnPageId = myWeb.moSession("pgid")
                    Case "DeletePage"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmDeletePage(myWeb.moRequest("pgid")))
                        If moAdXfm.valid Then
                            myWeb.msRedirectOnEnd = "?ewCmd=EditStructure&pgid=" & mnAdminTopLevel

                            'mcEwCmd = myWeb.moSession("ewCmd")
                            'sAdminLayout = ""
                            'oPageDetail.RemoveAll()
                            ''Redirect to previous page
                            'myWeb.mnPageId = myWeb.moSession("pgid")
                            myWeb.ClearPageCache()
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "MoveTop", "MoveUp", "MoveDown", "MoveBottom", "SortAlphaAsc", "SortAlphaDesc"
                        bLoadStructure = True
                        If myWeb.moRequest("id") <> "" And myWeb.moRequest("relId") = "" Then
                            'we are sorting content on a page
                            myWeb.moDbHelper.ReorderContent(CLng(myWeb.moRequest("pgid")), CLng(myWeb.moRequest("id")), myWeb.moRequest("ewCmd"), , myWeb.moRequest("position"))
                            If myWeb.moSession("lastPage") <> "" Then
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                myWeb.moSession("lastPage") = ""
                            Else
                                ' Edit page - just do the normal stuff
                                mcEwCmd = "Normal"
                                bAdminMode = False
                                sAdminLayout = ""
                                oPageDetail.RemoveAll()
                                myWeb.mnArtId = 0
                            End If

                        ElseIf myWeb.moRequest("relId") <> "" Then
                            'sorting Related Content for an item
                            myWeb.moDbHelper.ReorderContent(myWeb.moRequest("relId"), myWeb.moRequest("id"), myWeb.moRequest("ewCmd"), True)
                            If myWeb.moSession("lastPage") <> "" Then
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                myWeb.moSession("lastPage") = ""
                            Else
                                ' Edit page - just do the normal stuff
                                mcEwCmd = "Normal"
                                bAdminMode = False
                                sAdminLayout = ""
                                oPageDetail.RemoveAll()
                                myWeb.mnArtId = 0
                            End If
                        Else
                            'we are sorting the menu nodes
                            myWeb.moDbHelper.ReorderNode(dbHelper.objectTypes.ContentStructure, myWeb.moRequest("pgid"), myWeb.moRequest("ewCmd"))
                            mcEwCmd = myWeb.moSession("ewCmd")
                            If mcEwCmd = "EditStructure" Then
                                sAdminLayout = "EditStructure"
                                oPageDetail.RemoveAll()
                            Else
                                mcEwCmd = "Normal"
                                bAdminMode = False
                                sAdminLayout = ""
                                oPageDetail.RemoveAll()
                                myWeb.mnArtId = 0
                            End If
                            'Redirect to previous page
                            myWeb.mnPageId = myWeb.moSession("pgid")
                        End If
                        myWeb.ClearPageCache()
                    Case "ImageLib"
                        LibProcess(oPageDetail, sAdminLayout, fsHelper.LibraryType.Image)
                    Case "DocsLib"
                        LibProcess(oPageDetail, sAdminLayout, fsHelper.LibraryType.Documents)
                    Case "MediaLib"
                        LibProcess(oPageDetail, sAdminLayout, fsHelper.LibraryType.Media)
                    Case "ListUsers"
                        If IsNumeric(myWeb.moRequest("parid")) Then
                            myWeb.moSession("UserParId") = myWeb.moRequest("parid")
                        Else
                            If myWeb.moRequest("ewCmd") = "ListUsers" Then
                                myWeb.moSession("UserParId") = 0
                            End If
                        End If
                        Dim nStatus As Integer = 99
                        If myWeb.moRequest("status") <> "" Then
                            nStatus = CInt(myWeb.moRequest("status"))
                        End If
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory("User", CInt("0" & myWeb.moSession("UserParId")), nStatus))
                        sAdminLayout = "ListDirectory" '"ListUsers"
                        myWeb.moSession("ewCmd") = mcEwCmd

                    Case "ListUserContacts"

                        sAdminLayout = "ListUserContacts"
                        oPageDetail.AppendChild(myWeb.moDbHelper.GetUserContactsXml(CInt("0" & myWeb.moRequest("parid"))))
                    Case "EditUserContact"

                        sAdminLayout = "EditUserContact"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditDirectoryContact(CInt("0" & myWeb.moRequest("id")), CInt("0" & myWeb.moRequest("parid"))))
                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            mcEwCmd = "ListUserContacts"
                            GoTo ProcessFlow
                        End If
                    Case "AddUserContact"

                        sAdminLayout = "AddUserContact"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditDirectoryContact(CInt("0" & myWeb.moRequest("id")), CInt("0" & myWeb.moRequest("parid"))))
                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            mcEwCmd = "ListUserContacts"
                            GoTo ProcessFlow
                        End If
                    Case "DeleteUserContact"

                        myWeb.moDbHelper.DeleteObject(dbHelper.objectTypes.CartContact, CInt("0" & myWeb.moRequest("id")))
                        mcEwCmd = "ListUserContacts"
                        GoTo ProcessFlow
                    Case "ListCompanies"
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory("Company", CInt("0" & nParId)))

                        sAdminLayout = "ListDirectory" '"ListCompanies"
                        myWeb.moSession("ewCmd") = mcEwCmd
                        myWeb.moSession("nParId") = nParId

                    Case "ListDepartments"
                        If myWeb.moRequest("parid") > 0 Then
                            myWeb.moSession("DeptParId") = myWeb.moRequest("parid")
                        Else
                            If myWeb.moRequest("ewCmd") = "ListUsers" Then
                                myWeb.moSession("DeptParId") = 0
                            End If
                        End If
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory("Department", CInt("0" & myWeb.moSession("DeptParId"))))
                        sAdminLayout = "ListDirectory" '"ListDepartments"
                        myWeb.moSession("ewCmd") = mcEwCmd

                    Case "ListGroups"
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory("Group", CInt("0" & myWeb.moRequest("parid"))))
                        sAdminLayout = "ListDirectory" '"ListGroups"
                        myWeb.moSession("ewCmd") = mcEwCmd

                    Case "ListRoles"
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory("Role"))
                        sAdminLayout = "ListDirectory" '"ListRoles"
                        myWeb.moSession("ewCmd") = mcEwCmd

                    Case "ListDirectory"
                        If Not mcEwCmd2 Is Nothing Then
                            oPageDetail.AppendChild(myWeb.moDbHelper.listDirectory(mcEwCmd2, CInt("0" & myWeb.moRequest("parid"))))
                        End If

                        sAdminLayout = "ListDirectory" '"ListRoles"
                        myWeb.moSession("ewCmd") = mcEwCmd
                        myWeb.moSession("ewCmd2") = mcEwCmd2

                    Case "EditDirItem"
                        Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))
                        'oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmUserLogon("AdminLogon"))

                        oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmEditDirectoryItem(myWeb.moRequest("id"), myWeb.moRequest("dirType"), CLng("0" & myWeb.moRequest("parId"))))
                        If oMembershipProv.AdminXforms.valid Then
                            oPageDetail.RemoveAll()

                            'clear the listDirectory cache
                            myWeb.moDbHelper.clearDirectoryCache()

                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            mcEwCmd2 = myWeb.moSession("ewCmd2")
                            GoTo ProcessFlow

                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "EditRole"
                        'replaces admin menu with one with full permissions
                        GetAdminMenu()
                        oPageDetail.AppendChild(moAdXfm.xFrmEditRole(myWeb.moRequest("id")))
                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()

                            'clear the listDirectory cache
                            myWeb.moDbHelper.clearDirectoryCache()

                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            GoTo ProcessFlow

                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "ImpersonateUser"

                        'myWeb.moSession("ewAuth") = ""
                        'myWeb.moSession("adminMode") = ""
                        'myWeb.moSession("ewCmd") = ""

                        myWeb.moSession.RemoveAll()

                        Dim oMem As New Eonic.Web.Membership(myWeb)
                        If myWeb.moConfig("SecureMembershipAddress") <> "" Then
                            oMem.SecureMembershipProcess("logoffImpersonate")
                            myWeb.mnUserId = myWeb.moRequest("id")
                            myWeb.msRedirectOnEnd = myWeb.moConfig("SecureMembershipAddress") & moConfig("ProjectPath") & "/"
                        Else
                            myWeb.mnUserId = myWeb.moRequest("id")
                            myWeb.msRedirectOnEnd = moConfig("ProjectPath") & "/"
                        End If

                    Case "ResetUserAcct"
                        Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))

                        oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmResetAccount(myWeb.moRequest("id")))

                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            'clear the listDirectory cache
                            myWeb.moDbHelper.clearDirectoryCache()
                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            GoTo ProcessFlow
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "ResetUserPwd"
                        Dim oMembershipProv As New Providers.Membership.BaseProvider(myWeb, myWeb.moConfig("MembershipProvider"))

                        oPageDetail.AppendChild(oMembershipProv.AdminXforms.xFrmResetPassword(myWeb.moRequest("id")))

                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            'clear the listDirectory cache
                            myWeb.moDbHelper.clearDirectoryCache()
                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            GoTo ProcessFlow
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "UserIntegrations"
                        oPageDetail.AppendChild(moAdXfm.xFrmUserIntegrations(myWeb.moRequest("dirId"), mcEwCmd2))
                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            'clear the listDirectory cache
                            myWeb.moDbHelper.clearDirectoryCache()
                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            GoTo ProcessFlow
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "HideDirItem"
                        myWeb.moDbHelper.setObjectStatus(dbHelper.objectTypes.Directory, dbHelper.Status.Hidden, myWeb.moRequest("id"))
                        mcEwCmd = myWeb.moSession("ewCmd")
                        GoTo ProcessFlow
                    Case "DeleteDirItem"
                        If myWeb.moRequest("id") > 2 Then
                            oPageDetail.AppendChild(moAdXfm.xFrmDeleteDirectoryItem(myWeb.moRequest("id"), myWeb.moRequest("dirType")))
                            If moAdXfm.valid Then
                                oPageDetail.RemoveAll()

                                'clear the listDirectory cache
                                myWeb.moDbHelper.clearDirectoryCache()

                                'return to process flow
                                mcEwCmd = myWeb.moSession("ewCmd")
                                GoTo ProcessFlow

                            Else
                                sAdminLayout = "AdminXForm"
                            End If
                        End If
                    Case "RemoveDuplicateDirRelations"
                        myWeb.moDbHelper.RemoveDuplicateDirRelations()
                    Case "MaintainRelations"
                        oPageDetail.AppendChild(myWeb.moDbHelper.listDirRelations(myWeb.moRequest("id"), myWeb.moRequest("type"), CLng("0" & myWeb.moRequest("parId"))))
                        sAdminLayout = "ListDirRelations"

                    Case "SaveDirectoryRelations"
                        myWeb.moDbHelper.saveDirectoryRelations()

                        'return to process flow
                        mcEwCmd = myWeb.moSession("ewCmd")
                        bResetParId = True

                        GoTo ProcessFlow

                    Case "Permissions"
                        sAdminLayout = "EditStructurePermissions"
                        bLoadStructure = True

                    Case "DirPermissions"
                        If myWeb.moRequest("submit") <> "" Then
                            myWeb.moDbHelper.saveDirectoryPermissions()
                        End If
                        'impersonate User / Group
                        oPageDetail.AppendChild(oWeb.GetUserXML(myWeb.moRequest("parid")))
                        oPageDetail.AppendChild(oWeb.GetStructureXML(CLng(myWeb.moRequest("parid")), Me.mnAdminTopLevel, 0, "", False, False, False, True, False, "", ""))
                        sAdminLayout = "EditDirectoryItemPermissions"

                    Case "DirMemberships"
                        'Add specified child members to a directory entity.
                        oPageDetail.AppendChild(moAdXfm.xFrmDirMemberships(myWeb.moRequest("type"), myWeb.moRequest("id"), myWeb.moRequest("parId"), myWeb.moRequest("childTypes")))
                        If moAdXfm.valid Then
                            oPageDetail.RemoveAll()
                            'return to process flow
                            mcEwCmd = myWeb.moSession("ewCmd")
                            GoTo ProcessFlow
                        Else
                            sAdminLayout = "AdminXForm"
                        End If

                    Case "EditPagePermissions"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmPagePermissions(myWeb.moRequest("pgid")))
                        myWeb.ClearPageCache()
                        sAdminLayout = "AdminXForm"

                    Case "EditPageRights"
                        bLoadStructure = True
                        oPageDetail.AppendChild(moAdXfm.xFrmPageRights(myWeb.moRequest("pgid")))
                        sAdminLayout = "AdminXForm"
                        '++++++++++++++++++++++++++ ECOMMERCE ++++++++++++++++++++++++++++++++
                    Case "Ecommerce"
                        'placeholder for ecommerce dashboard
                    Case "CartActivity", "CartReports", "CartActivityDrilldown", "CartActivityPeriod", "CartDownload"
                        OrderProcess(oPageDetail, sAdminLayout, "")
                    Case "Orders", "OrdersShipped", "OrdersFailed", "OrdersDeposit", "OrdersHistory", "OrdersAwaitingPayment"
                        OrderProcess(oPageDetail, sAdminLayout, "Order")
                    Case "Quotes", "QuotesFailed", "QuotesDeposit", "QuotesHistory"
                        OrderProcess(oPageDetail, sAdminLayout, "Quote")

                    Case "ShippingLocations"
                        ShippingLocationsProcess(oPageDetail, sAdminLayout)

                    Case "DeliveryMethods"
                        DeliveryMethodProcess(oPageDetail, sAdminLayout)

                    Case "PaymentProviders"
                        PaymentProviderProcess(oPageDetail, sAdminLayout)

                    Case "Carriers"
                        CarriersProcess(oPageDetail, sAdminLayout)

                    Case "CartSettings"

                        oPageDetail.AppendChild(moAdXfm.xFrmCartSettings())
                        If moAdXfm.valid Then
                            mcEwCmd = "Normal"
                        Else
                            sAdminLayout = "AdminXForm"
                        End If
                    Case "SalesReports"



                    Case "EditStructure", "QuizReports", "ListQuizes"
                        bLoadStructure = True
                        myWeb.moSession("lastPage") = myWeb.mcOriginalURL
                        'do nothing

                    Case "QuizReports", "ListQuizes"
                        'This should be moved to EonicLMS
                        bLoadStructure = True

                    Case "ManagePollVotes"
                        PollsProcess(oPageDetail, sAdminLayout)

                    Case "ManageLookups"

                        ManageLookups(oPageDetail, sAdminLayout)




                        'if the command for turning on the preview mode is sent then
                        'check if we are in admin mode, if so, turn preview on
                    Case "PreviewOn"
                        sAdminLayout = ""
                        mbPreviewMode = True
                        If myWeb.moSession("PreviewDate") Is Nothing Then
                            myWeb.moSession("PreviewDate") = Now.Date
                        End If

                        If myWeb.moSession("PreviewUser") Is Nothing Then
                            myWeb.moSession("PreviewUser") = oWeb.mnUserId
                        End If

                        If IsDate(myWeb.moRequest("PreviewDate")) Then
                            myWeb.moSession("PreviewDate") = CDate(myWeb.moRequest("PreviewDate"))
                        End If
                        myWeb.mdDate = myWeb.moSession("PreviewDate")

                        If CInt("0" & myWeb.moRequest("PreviewUser")) > 0 Then
                            myWeb.moSession("PreviewUser") = CInt("0" & myWeb.moRequest("PreviewUser"))
                        End If
                        myWeb.mnUserId = myWeb.moSession("PreviewUser")

                    Case "RelateSearch"
                        If ButtonSubmitted(myWeb.moRequest, "saveRelated") Then
                            'code for saving results of 2nd form submission
                            myWeb.moDbHelper.saveContentRelations()
                            'redirect to the parent content xform
                            myWeb.moSession("ewCmd") = ""
                            If myWeb.moRequest("redirect") = "normal" Then
                                If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                                    myWeb.msRedirectOnEnd = "?ewCmd=" & mcEwCmd & "&pgid=" & myWeb.mnPageId 'myWeb.moSession("lastPage")
                                Else
                                    myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                                End If
                            Else
                                myWeb.msRedirectOnEnd = myWeb.moRequest.QueryString("Path") & "?ewCmd=EditContent&id=" & myWeb.moRequest.Form.Get("id") & IIf(myWeb.moRequest.QueryString("pgid") = "", "", "&pgid=" & myWeb.moRequest.QueryString("pgid"))
                            End If
                        Else
                            'Process for related content
                            Dim nRelParent As Long = CLng("0" & myWeb.moRequest("RelParent"))
                            Dim redirect As String = ""
                            If nRelParent = 0 Then
                                nRelParent = CLng("0" & myWeb.moSession("mcRelParent"))
                            Else
                                redirect = "normal"
                            End If
                            oPageDetail.AppendChild(moAdXfm.xFrmFindRelated(nRelParent, myWeb.moRequest.QueryString("type"), oPageDetail, "nParentContentId", False, "tblcontentRelation", "nContentChildId", "nContentParentId", redirect))
                            sAdminLayout = "RelatedSearch"
                        End If
                        If moAdXfm.valid Then

                        End If

                    Case "LocateSearch"
                        'Process for related content
                        bLoadStructure = True
                        moAdXfm.xFrmFindContentToLocate(myWeb.moRequest("pgid"), myWeb.moRequest("nFromPage"), myWeb.moRequest("bIncludeChildren"), myWeb.moRequest("type"), myWeb.moRequest("cSearchTerm"), oPageDetail)
                        sAdminLayout = "LocateSearch"
                        If moAdXfm.valid Then
                            sAdminLayout = ""
                            oPageDetail.RemoveAll()
                            If mcEwCmd = "Normal" Or mcEwCmd = "NormalMail" Then
                                myWeb.msRedirectOnEnd = "?ewCmd=" & mcEwCmd & "&pgid=" & myWeb.mnPageId 'myWeb.moSession("lastPage")
                            Else
                                myWeb.msRedirectOnEnd = myWeb.moSession("lastPage")
                            End If
                            'myWeb.msRedirectOnEnd = "?pgid=" & myWeb.moRequest("pgid")
                            myWeb.moSession("ewCmd") = ""
                        End If
                    Case "cleanLocation"
                        If bAdminMode Then myWeb.moDbHelper.cleanLocations()
                    Case "ProductGroups"
                        ProductGroupsProcess(oPageDetail, sAdminLayout, IIf(IsNumeric(myWeb.moRequest.QueryString("GrpID")), myWeb.moRequest.QueryString("GrpID"), 0))
                    Case "AddProductGroups", "EditProductGroups"
                        bLoadStructure = True
                        sAdminLayout = "AdminXForm"
                        oPageDetail.AppendChild(moAdXfm.xFrmProductGroup(IIf(IsNumeric(myWeb.moRequest.QueryString("GroupId")), myWeb.moRequest.QueryString("GroupId"), 0)))
                        If moAdXfm.valid Then
                            mcEwCmd = "ProductGroups"
                            GoTo ProcessFlow
                        End If
                    Case "DeleteProductGroups"
                        myWeb.moDbHelper.DeleteObject(dbHelper.objectTypes.CartProductCategories, myWeb.moRequest.QueryString("GroupId"))
                        mcEwCmd = "ProductGroups"
                        GoTo ProcessFlow
                    Case "AddProductGroupsProduct"
                        bLoadStructure = True
                        sAdminLayout = "LocateContent"
                        If ButtonSubmitted(myWeb.moRequest, "saveRelated") Then
                            myWeb.moDbHelper.saveProductsGroupRelations()
                            myWeb.msRedirectOnEnd = myWeb.moRequest.QueryString("Path") & "?ewCmd=ProductGroups&GroupId=" & myWeb.moRequest.QueryString("GroupId")
                        Else
                            Dim sProductTypes As String = "Product,SKU,Ticket"
                            If moConfig("ProductTypes") <> "" Then
                                sProductTypes = moConfig("ProductTypes")
                            End If

                            oPageDetail.AppendChild(moAdXfm.xFrmFindRelated(myWeb.moRequest.QueryString("GroupId"), sProductTypes, oPageDetail, myWeb.moRequest.QueryString("GroupId"), True, "tblCartCatProductRelations", "nContentId", "nCatId"))
                            sAdminLayout = "RelatedSearch"
                        End If
                    Case "RemoveProductGroupsProduct"
                        myWeb.moDbHelper.DeleteObject(dbHelper.objectTypes.CartCatProductRelations, myWeb.moRequest.QueryString("RelId"))
                        mcEwCmd = "ProductGroups"
                        GoTo ProcessFlow
                    Case "DiscountRules", "EditDiscountRules"
                        If (ButtonSubmitted(myWeb.moRequest, "addNewDiscountRule") And IsNumeric(myWeb.moRequest.Form("newDiscountType"))) _
                        Or ButtonSubmitted(myWeb.moRequest, "ewSubmit") _
                        Or mcEwCmd = "EditDiscountRules" Then
                            bLoadStructure = True
                            sAdminLayout = "AdminXForm"
                            Dim nDiscountType As Long = IIf(IsNumeric(myWeb.moRequest.Form("newDiscountType")), myWeb.moRequest.Form("newDiscountType"), 0)
                            nDiscountType = IIf(IsNumeric(myWeb.moRequest.Form("nDiscountCat")), myWeb.moRequest.Form("nDiscountCat"), nDiscountType)

                            oPageDetail.AppendChild(moAdXfm.xFrmDiscountRule(IIf(IsNumeric(myWeb.moRequest.QueryString("DiscId")), myWeb.moRequest.QueryString("DiscId"), 0), nDiscountType))

                            If moAdXfm.valid Then
                                myWeb.ClearPageCache()
                                DiscountRulesProcess(oPageDetail, sAdminLayout)
                            End If
                        Else
                            DiscountRulesProcess(oPageDetail, sAdminLayout)
                        End If

                    Case "RemoveDiscountRules"
                        myWeb.moDbHelper.DeleteObject(dbHelper.objectTypes.CartDiscountRules, myWeb.moRequest.QueryString("DiscId"))
                        mcEwCmd = "DiscountRules"
                        GoTo ProcessFlow
                    Case "ApplyDirDiscountRules"
                        bLoadStructure = True
                        sAdminLayout = "AdminXForm"
                        oPageDetail.AppendChild(moAdXfm.xFrmDiscountDirRelations(myWeb.moRequest.QueryString("DiscId"), ""))
                    Case "ApplyGrpDiscountRules"
                        bLoadStructure = True
                        sAdminLayout = "AdminXForm"
                        oPageDetail.AppendChild(moAdXfm.xFrmDiscountProductRelations(myWeb.moRequest.QueryString("DiscId"), ""))
                    Case "SiteIndex"
                        bLoadStructure = True
                        sAdminLayout = "AdminXForm"
                        oPageDetail.AppendChild(moAdXfm.xFrmStartIndex())
                        '-- Call all of the process for the newsletter functionaltiy
                    Case "MailingList", "NormalMail", "MailPreviewOn", "AdvancedMail", "AddMailModule", "EditMail", "EditMailLayout", "NewMail", "PreviewMail", "SendMail", "SendMailPersonalised", "SendMailunPersonalised", "MailHistory", "MailOptOut", "ProcessMailbox", "DeletePageMail", "SyncMailList", "ListMailLists"
                        bMailMenu = True

                        Dim moMailConfig As System.Collections.Specialized.NameValueCollection = WebConfigurationManager.GetWebApplicationSection("eonic/mailinglist")
                        Dim sMessagingProvider As String = ""
                        If Not moMailConfig Is Nothing Then
                            sMessagingProvider = moMailConfig("MessagingProvider")
                        End If
                        Dim oMessaging As New Eonic.Providers.Messaging.BaseProvider(myWeb, sMessagingProvider)
                        Dim passthroughCmd As String = mcEwCmd
                        If mcEwCmd2 <> "" Then
                            passthroughCmd = passthroughCmd & "." + mcEwCmd2
                        End If

                        'If oMessaging.AdminProcess.MailingListProcess(oPageDetail, oWeb, sAdminLayout, myWeb.moRequest("ewCmd"), bLoadStructure, EditContext, clearEditContext) = "GoTo" Then GoTo ProcessFlow
                        If oMessaging.AdminProcess.MailingListProcess(oPageDetail, oWeb, sAdminLayout, passthroughCmd, bLoadStructure, EditContext, clearEditContext) = "GoTo" Then GoTo ProcessFlow

                        mcEwCmd = passthroughCmd

                        If sAdminLayout = "Preview" Then
                            sAdminLayout = ""
                            mbPreviewMode = True
                        End If

                        oMessaging = Nothing

                    Case "ScheduledItems", "AddScheduledItem", "EditScheduledItem", "DeactivateScheduledItem", "ActivateScheduledItem", "ScheduledItemRunNow"
                        SchedulerProcess(mcEwCmd, sAdminLayout, oPageDetail)
                        bLoadStructure = True

                    Case "SystemPages"
                        bSystemPagesMenu = True
                        sAdminLayout = "SystemPages"
                        oWeb.mbAdminMode = True
                        sAdminLayout = "SystemPages"
                        oWeb.mnPageId = oWeb.mnSystemPagesId
                        'oWeb.mnSystemPagesId = getPageIdFromPath("System+Pages", False, False)



                    Case "ViewSystemPages"
                        bSystemPagesMenu = True
                        oWeb.mnSystemPagesId = myWeb.moDbHelper.getPageIdFromPath("System+Pages", False, False)
                        oWeb.mbAdminMode = False
                        If Not myWeb.mbSuppressLastPageOverrides Then myWeb.moSession("lastPage") = "?ewCmd=ViewSystemPages&pgid=" & myWeb.mnPageId

                    Case "Subscriptions", "AddSubscriptionGroup", "EditSubscriptionGroup", "AddSubscription", "EditSubscription", "MoveSubscription", "LocateSubscription", "UpSubscription", "DownSubscription", "ListSubscribers"
                        SubscriptionProcess(mcEwCmd, sAdminLayout, oPageDetail)
                        bLoadStructure = True

                    Case "EditXForm"
                        EditXFormProcess(sAdminLayout, oPageDetail, bLoadStructure)

                    Case "Reports"
                        ReportsProcess(oPageDetail, sAdminLayout)

                End Select

                SupplimentalProcess(sAdminLayout, oPageDetail)

                ' Supplimental Process check for Go To Process Flow
                If sAdminLayout = "GoToProcessFlow" Then
                    GoTo ProcessFlow
                End If

                'we want to persist the cmd if we are in normal, advanced mode, so we can navigate
                If mcEwCmd = "Normal" Or mcEwCmd = "PreviewOn" _
                Or mcEwCmd = "ViewSystemPages" Then
                    myWeb.moSession("ewCmd") = mcEwCmd
                    bAdminMode = False
                    sAdminLayout = ""
                    'ElseIf mcEwCmd = "AddContent" Or mcEwCmd = "EditContent" Then
                    '    myWeb.moSession("ewCmd") = mcEwCmd
                    '    bAdminMode = False
                    '    sAdminLayout = ""
                ElseIf mcEwCmd = "NormalMail" Then
                    myWeb.moSession("ewCmd") = mcEwCmd
                    bAdminMode = False
                    sAdminLayout = ""
                ElseIf mcEwCmd = "EditPageSEO" Then
                    bAdminMode = True
                    myWeb.moSession("ewCmd") = mcEwCmd
                ElseIf mcEwCmd = "EditPage" Then
                    bAdminMode = True
                    sAdminLayout = "PageSettings"
                    myWeb.moSession("ewCmd") = mcEwCmd
                ElseIf mcEwCmd = "Advanced" Or mcEwCmd = "EditStructure" Or mcEwCmd = "EditPage" Then
                    bAdminMode = True
                    sAdminLayout = mcEwCmd
                    myWeb.moSession("ewCmd") = mcEwCmd
                ElseIf mcEwCmd = "SystemPages" Then
                    myWeb.moSession("ewCmd") = mcEwCmd
                    bAdminMode = True
                    bSystemPagesMenu = True
                    'basically an exact copy of what is going on above
                    'not the best, but only way to do it
                    sAdminLayout = "SystemPages"
                    oWeb.mnSystemPagesId = myWeb.moDbHelper.getPageIdFromPath("System+Pages", False, False)
                    If oWeb.mnSystemPagesId = gnTopLevel Then
                        '   we have no System Pages page we better create one.
                        oWeb.mnSystemPagesId = myWeb.moDbHelper.insertStructure(0, "", "System Pages", "", "Column1")
                    End If
                    oWeb.mnPageId = oWeb.mnSystemPagesId
                    sAdminLayout = "SystemPages"

                End If

                myWeb.moSession("editContext") = EditContext

                'reset the parid after go to processflow redirects
                If bResetParId = True Then
                    myWeb.moSession("nParId") = "0"
                End If


                If bMailMenu Or mcEwCmd = "NormalMail" Then
                    If moConfig("MailingList") = "on" Then
                        Dim moMailConfig As System.Collections.Specialized.NameValueCollection = WebConfigurationManager.GetWebApplicationSection("eonic/mailinglist")
                        If Not moMailConfig Is Nothing Then
                            oWeb.mnMailMenuId = moMailConfig("RootPageId")
                            'oWeb.GetStructureXML("Newsletter", , moMailConfig("RootPageId"))
                        End If
                    End If
                End If
                If bSystemPagesMenu Then
                    oWeb.mnSystemPagesId = myWeb.moDbHelper.getPageIdFromPath("System+Pages", False, False)
                End If


                'We have done all our updating lets get new pagexml
                If mcEwCmd = "Normal" Or mcEwCmd = "Advanced" Or mcEwCmd = "PreviewOn" Or mcEwCmd = "MailPreviewOn" Or mcEwCmd = "EditXForm" Or mcEwCmd = "EditPage" Or mcEwCmd = "EditPageSEO" _
                Or mcEwCmd = "NormalMail" Or mcEwCmd = "AdvancedMail" Or mcEwCmd = "NewMail" Or mcEwCmd = "MailingList" Or mcEwCmd = "SystemPages" Or mcEwCmd = "ViewSystemPages" _
                Or mcEwCmd = "LocateContent" Or mcEwCmd = "MoveContent" Or mcEwCmd = "LocateContentDetail" Then
                    'TS: removed 4 jul 2014 do not need to load page content when editing
                    ' Or mcEwCmd = "EditContent" Or mcEwCmd = "CopyContent" Or mcEwCmd = "AddContent" Then
                    If mcEwCmd = "PreviewOn" Or mcEwCmd = "MailPreviewOn" Then
                        oWeb.mbAdminMode = False
                    Else
                        oWeb.mbAdminMode = True
                    End If
                    'no need to get page xml if redirecting, speeds up editing on pages with loads of content.
                    If myWeb.msRedirectOnEnd = "" Then
                        oWeb.GetPageXML()
                    End If
                Else

                    getAdminXML(oWeb, bLoadStructure)
                End If

                moPageXML = oWeb.moPageXml
                moPageXML.DocumentElement.SetAttribute("title", sPageTitle)

                If Not oPageDetail Is Nothing Then
                    If moPageXML.DocumentElement.SelectSingleNode("ContentDetail") Is Nothing Then
                        If oPageDetail.InnerXml <> "" Then
                            moPageXML.DocumentElement.AppendChild(oPageDetail)
                        End If
                    Else
                        If oPageDetail.InnerXml <> "" Then
                            moPageXML.DocumentElement.ReplaceChild(oPageDetail, moPageXML.DocumentElement.SelectSingleNode("ContentDetail"))
                        End If
                    End If
                End If

                If sAdminLayout <> "" Then
                    moPageXML.DocumentElement.SetAttribute("layout", sAdminLayout)
                End If

                If bClearEditContext Then
                    myWeb.moSession("editContext") = Nothing
                    EditContext = ""
                End If

                If EditContext <> "" Then
                    moPageXML.DocumentElement.SetAttribute("editContext", EditContext)
                    myWeb.moSession("editContext") = EditContext
                End If



                If mbPreviewMode Then
                    moPageXML.DocumentElement.SetAttribute("previewMode", LCase(mbPreviewMode.ToString))
                    If Not moPageXML.SelectSingleNode("AdminMenu") Is Nothing Then
                        moPageXML.RemoveChild(moPageXML.SelectSingleNode("AdminMenu"))
                    End If

                    If mcEwCmd <> "MailPreviewOn" Then
                        GetPreviewMenu()
                    End If
                Else
                    moPageXML.DocumentElement.SetAttribute("adminMode", LCase(bAdminMode.ToString))
                    If bAdminMode Then
                        '    moPageXML.DocumentElement.SetAttribute("cssFramework", "bs3")
                    End If
                    ' TS perhaps we should Strip out cms with no rights here
                    ' GetAdminMenu()
                End If

                moPageXML.DocumentElement.SetAttribute("ewCmd", mcEwCmd)
                If Not String.IsNullOrEmpty(mcEwCmd2) Then moPageXML.DocumentElement.SetAttribute("ewCmd2", mcEwCmd2)
                If Not String.IsNullOrEmpty(mcEwCmd3) Then moPageXML.DocumentElement.SetAttribute("ewCmd3", mcEwCmd3)

                If myWeb.moRequest("parid") <> "" Then
                    moPageXML.DocumentElement.SetAttribute("parId", myWeb.moRequest("parid"))
                End If


            Catch ex As Exception
                returnException(mcModuleName, "adminProcess", ex, "", sProcessInfo, gbDebug)
            Finally

            End Try

        End Sub

        Public Overridable Sub adminAccessRights()
            Dim processInfo As String = ""
            Dim oUserXml As XmlElement
            Dim oMenuElmt As XmlElement
            Dim deleteCmds As Hashtable = New Hashtable
            Dim pagePermLevel As String

            Try

                'Get the admin menu for the site
                GetAdminMenu()

                'get the user permissions
                myWeb.RefreshUserXML()

                oUserXml = moPageXML.SelectSingleNode("/Page/User")

                pagePermLevel = oUserXml.GetAttribute("pagePermission")
                'Are you a domain user if so you are god !

                'RJP 7 Nov 2012. Added LCase to MembershipEncryption.
                If Not myWeb.moSession("ewAuth") = Eonic.Tools.Encryption.HashString(myWeb.moSession.SessionID & moConfig("AdminPassword"), LCase(myWeb.moConfig("MembershipEncryption")), True) Then

                    'Are you an administrator user with no AdminRights yet set then you too are god ! this to cater for existing sites
                    If oUserXml.SelectSingleNode("Role[@name='Administrator' and @isMember='yes' and not(AdminRights)]") Is Nothing Then

                        'Otherwise step throught the admin menu and remove stuff
                        For Each oMenuElmt In myWeb.moPageXml.SelectNodes("/Page/AdminMenu/descendant-or-self::*")

                            Dim ewCmd As String = oMenuElmt.GetAttribute("cmd")
                            If Not ewCmd = "" Then
                                'do you have permissions on the current ewCmd ?
                                If oUserXml.SelectSingleNode("descendant-or-self::MenuItem[@cmd='" & ewCmd & "' and @adminRight='true']") Is Nothing Then

                                    'give some info about feature denied to user
                                    moDeniedAdminMenuElmt = oMenuElmt.CloneNode(False)

                                    oMenuElmt.SetAttribute("adminRight", "false")

                                    If deleteCmds(ewCmd) Is Nothing Then deleteCmds.Add(ewCmd, ewCmd)

                                    If mcEwCmd = ewCmd Then
                                        'Set the ewCmd to "adminDenied"
                                        mcEwCmd = "adminDenied"
                                        Exit Sub
                                    Else
                                        moDeniedAdminMenuElmt = Nothing
                                    End If
                                Else
                                    'Clever stuff for page level editing rights

                                    Select Case pagePermLevel

                                        'Denied = 0 
                                        'Open = 1 
                                        'View = 2
                                        'Add = 3
                                        'AddUpdateOwn = 4
                                        'UpdateAll = 5
                                        'Approve = 6
                                        'AddUpdateOwnPublish = 7
                                        'Publish = 8
                                        'Full = 9

                                        Case "Full", "Publish", "AddUpdateOwnPublish", "Approve", "AddUpdateOwn", "UpdateAll"
                                            'do nothing alls good
                                        Case "Add"
                                            Select Case ewCmd
                                                Case "EditContent", "DeleteContent", "EditModule", "AddModule", "CopyContent", "LocateContent", "MoveContent"
                                                    If mcEwCmd = ewCmd Then
                                                        moDeniedAdminMenuElmt = oMenuElmt.CloneNode(False)
                                                        mcEwCmd = "adminDenied"
                                                        Exit Sub
                                                    End If
                                                    If deleteCmds(ewCmd) Is Nothing Then deleteCmds.Add(ewCmd, ewCmd)
                                            End Select
                                        Case Else
                                            Select Case ewCmd
                                                Case "EditContent", "AddContent", "EditModule", "AddModule", "DeleteContent", "CopyContent", "LocateContent", "MoveContent"
                                                    If mcEwCmd = ewCmd Then
                                                        moDeniedAdminMenuElmt = oMenuElmt.CloneNode(False)
                                                        mcEwCmd = "adminDenied"
                                                        Exit Sub
                                                    End If
                                                    If deleteCmds(ewCmd) Is Nothing Then deleteCmds.Add(ewCmd, ewCmd)
                                            End Select
                                    End Select
                                    'Clever stuff for page level content type editing rights (Where would this be stored ?)
                                End If
                            End If

                        Next

                        Dim key As String
                        For Each key In deleteCmds.Keys
                            processInfo = "deleting " & deleteCmds(key) & " from admin menu"
                            oMenuElmt = myWeb.moPageXml.SelectSingleNode("/Page/AdminMenu/descendant-or-self::*[@cmd='" & deleteCmds(key) & "']")
                            If Not oMenuElmt Is Nothing Then oMenuElmt.ParentNode.RemoveChild(oMenuElmt)
                        Next


                    End If

                End If


            Catch ex As Exception
                returnException(mcModuleName, "adminAccessRights", ex, "", processInfo, gbDebug)
            Finally

            End Try
        End Sub


        Public Overridable Sub EditXFormProcess(ByRef adminLayout As String, ByRef pageDetail As XmlElement, ByRef loadStructure As Boolean)
            Dim processInfo As String = ""
            Dim editor As XFormEditor
            Dim contentId As Long = 0
            Dim xfrm As XmlElement
            Dim skip As Boolean = False
            Dim saveInstance As Boolean = False

            Dim ref As String = myWeb.moRequest("ref")

            Try

                If myWeb.moRequest("artid") IsNot Nothing AndAlso IsNumeric(myWeb.moRequest("artid") & "") Then
                    contentId = CLng(myWeb.moRequest("artid") & "")
                End If

                editor = myWeb.GetXformEditor(contentId)

                Select Case mcEwCmd2

                    Case Nothing
                        adminLayout = "AdminXForm"

                    Case "EditGroup"
                        loadStructure = True
                        xfrm = editor.xFrmEditXFormGroup(ref)
                        If editor.valid Then
                            mcEwCmd = "EditXForm"
                            mcEwCmd2 = ""
                            saveInstance = True
                        Else
                            pageDetail.AppendChild(pageDetail.OwnerDocument.ImportNode(xfrm, True))
                        End If

                    Case "AddGroup"
                        loadStructure = True
                        xfrm = editor.xFrmEditXFormGroup("", ref)
                        If editor.valid Then
                            mcEwCmd = "EditXForm"
                            mcEwCmd2 = ""
                            saveInstance = True
                        Else
                            pageDetail.AppendChild(pageDetail.OwnerDocument.ImportNode(xfrm, True))
                        End If

                    Case "DeleteElement"

                        loadStructure = True
                        'For Deleting xForm Elements
                        xfrm = editor.xFrmDeleteElement(myWeb.moRequest("ref"), myWeb.moRequest("pos"))
                        If editor.valid Then
                            mcEwCmd = "EditXForm"
                            mcEwCmd2 = ""
                            saveInstance = True
                        Else
                            pageDetail.AppendChild(pageDetail.OwnerDocument.ImportNode(xfrm, True))
                        End If

                    Case "MoveTop", "MoveUp", "MoveDown", "MoveBottom"
                        loadStructure = True
                        editor.moveElement(myWeb.moRequest("artid"), myWeb.moRequest("ref"), mcEwCmd2)
                        mcEwCmd = "EditXForm"
                        saveInstance = True

                    Case "MoveItemTop", "MoveItemUp", "MoveItemDown", "MoveItemBottom", "DeleteItem"
                        loadStructure = True
                        editor.moveElement(myWeb.moRequest("artid"), myWeb.moRequest("ref"), mcEwCmd2, myWeb.moRequest("pos"))
                        mcEwCmd = "EditXForm"
                        mcEwCmd2 = ""
                        saveInstance = True

                    Case "EditInput", "EditInput"

                        loadStructure = True
                        xfrm = editor.xFrmEditXFormInput(myWeb.moRequest("ref"), myWeb.moRequest("parref"), myWeb.moRequest("type"))
                        If editor.valid Then
                            mcEwCmd = "EditXForm"
                            mcEwCmd2 = ""
                            saveInstance = True
                        Else
                            pageDetail.AppendChild(pageDetail.OwnerDocument.ImportNode(xfrm, True))
                        End If


                    Case "EditItem", "AddItem"
                        loadStructure = True
                        xfrm = editor.xFrmEditXFormItem(myWeb.moRequest("ref"), CLng(myWeb.moRequest("pos")))
                        If editor.valid Then
                            mcEwCmd = "EditXForm"
                            mcEwCmd2 = ""
                            saveInstance = True
                        Else
                            pageDetail.AppendChild(pageDetail.OwnerDocument.ImportNode(xfrm, True))
                        End If

                    Case Else
                        skip = True
                End Select

                If Not skip Then
                    adminLayout = "AdminXForm"

                    If saveInstance Then
                        'save the xform back in the database
                        myWeb.moDbHelper.setObjectInstance(dbHelper.objectTypes.Content, editor.MasterInstance)
                    End If
                End If

                xfrm = Nothing

                'This is just a placeholder for overloading
            Catch ex As Exception
                returnException(mcModuleName, "EditXFormProcess", ex, "", processInfo, gbDebug)
            Finally

            End Try
        End Sub

        Public Overridable Sub SupplimentalProcess(ByRef sAdminLayout As String, ByRef oPageDetail As XmlElement)

            'This is just a placeholder for overloading

        End Sub

        Public Sub FileImportProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim sErrorMsg As String = ""
            Try

                sAdminLayout = "FileImport"
                Dim sImportLoaction As String = "..\imports\"


                oPageDetail.AppendChild(moAdXfm.xFrmImportFile(sImportLoaction))
                If moAdXfm.valid Then

                    Dim cXsltPath As String = moAdXfm.Instance.SelectSingleNode("file/@importXslt").InnerText
                    Dim cFilePath As String = moAdXfm.Instance.SelectSingleNode("file/@filename").InnerText

                    'first we take our take our xls and convert to xml
                    Dim oImportXml As New XmlDocument

                    If cFilePath.EndsWith(".xls") Or cFilePath.EndsWith(".xlsx") Or cFilePath.EndsWith(".csv") Then

                        Dim oConvert As New Eonic.Tools.Conversion(Eonic.Tools.Conversion.Type.Excel, Eonic.Tools.Conversion.Type.Xml, cFilePath)
                        oConvert.Convert()
                        If oConvert.State = Eonic.Tools.Conversion.Status.Succeeded Then
                            oImportXml.LoadXml(oConvert.Output.OuterXml)
                        Else
                            moAdXfm.valid = False
                            moAdXfm.addNote(moAdXfm.moXformElmt, xForm.noteTypes.Alert, oConvert.Message)
                            sProcessInfo = oConvert.Message
                        End If

                        oImportXml.LoadXml(oConvert.Output.OuterXml)
                    ElseIf cFilePath.EndsWith(".xml") Then
                        '  cFilePath = myWeb.goServer.MapPath(cFilePath)
                        oImportXml.Load(cFilePath)

                        Dim oImportRootElmt As XmlElement = oImportXml.DocumentElement

                        If oImportRootElmt.Name = "DatabaseImport" Then
                            Dim DBConn As String = "Data Source=" & oImportRootElmt.GetAttribute("databaseServer") & "; " & _
                    "Initial Catalog=" & oImportRootElmt.GetAttribute("databaseName") & "; " & _
                    "user id=" & oImportRootElmt.GetAttribute("databaseUsername") & "; password=" & oImportRootElmt.GetAttribute("databasePassword")

                            Dim newDb As New dbHelper(DBConn, myWeb.mnUserId, myWeb.moCtx)
                            If newDb.ConnectionValid = False Then
                                moAdXfm.valid = False
                                sErrorMsg = "Bad DB Connection - " & DBConn
                            Else
                                Dim ImportDS As New DataSet
                                Dim sSql As String = oImportRootElmt.GetAttribute("select")
                                If sSql = "" Then
                                    sSql = "select * from " & oImportRootElmt.GetAttribute("tableName")
                                End If
                                ImportDS = newDb.GetDataSet(sSql, oImportRootElmt.GetAttribute("tableName"))
                                oImportXml.LoadXml(ImportDS.GetXml())
                            End If
                        
                        End If
                    End If

                    'converted successfully to xml
                    If sErrorMsg = "" Then

                        If Not oImportXml.OuterXml = "" Then

                            'lets just output our source xml
                            Dim oPreviewElmt As XmlElement = moPageXML.CreateElement("PreviewFileXml")
                            oPreviewElmt.InnerXml = oImportXml.OuterXml
                            oPageDetail.AppendChild(oPreviewElmt)

                            'NB: Old Tools Transform----------------
                            'then we transform to our standard import XML
                            'Dim oTransform As New Eonic.Tools.Xslt.Transform
                            'Dim moXSLTFunctions As New Tools.Xslt.XsltFunctions
                            'oTransform.Xml = oImportXml
                            'oTransform.XslTExtensionObject = moXSLTFunctions
                            'oTransform.XslTExtensionURN = "ew"

                            'oTransform.XslTFile = myWeb.goServer.MapPath("/xsl/import/" & cXsltPath)
                            'Dim cInstancesXml As String = oTransform.Process()
                            'NB: ----------------

                            'NB: New (Web) Transform
                            Dim styleFile As String = CStr(myWeb.goServer.MapPath("/xsl/import/" & cXsltPath))
                            Dim oTransform As New Eonic.XmlHelper.Transform(myWeb, styleFile, False)
                            PerfMon.Log("Admin", "FileImportProcess-startxsl")
                            oTransform.mbDebug = gbDebug
                            oTransform.ProcessDocument(oImportXml)
                            PerfMon.Log("Admin", "FileImportProcess-endxsl")
                            'We display the results
                            Dim oPreviewElmt2 As XmlElement = moPageXML.CreateElement("PreviewImport")
                            If oTransform.HasError Then
                                oPreviewElmt2.SetAttribute("errorMsg", oTransform.currentError.Message)
                                oPreviewElmt2.InnerText = oTransform.currentError.StackTrace
                            Else
                                oPreviewElmt2.InnerXml = oImportXml.InnerXml

                            End If
                            oPageDetail.AppendChild(oPreviewElmt2)
                            oTransform = Nothing

                            'We save to database if OK
                            Dim cOppMode As String = moAdXfm.Instance.SelectSingleNode("file/@opsMode").InnerText
                            If cOppMode = "import" Then
                                'here we go lets do the do...!! whoah!
                                myWeb.moDbHelper.importObjects(oPreviewElmt2.FirstChild)

                            End If

                        Else
                            moAdXfm.valid = False
                            sErrorMsg = "No Content Returned"
                            moAdXfm.addNote(moAdXfm.moXformElmt, xForm.noteTypes.Alert, sErrorMsg)
                        End If
                    Else

                        moAdXfm.addNote(moAdXfm.moXformElmt, xForm.noteTypes.Alert, sErrorMsg)

                    End If

                End If


            Catch ex As Exception
                returnException(mcModuleName, "MemberActivityProcess", ex, "", "", gbDebug)
            End Try
        End Sub

        Public Overridable Sub SupplimentalAdminMenu(ByRef oAdminMenu As XmlElement)

            'This is just a placeholder for overloading

        End Sub

        Private Sub getAdminXML(ByRef oWeb As Eonic.Web, Optional ByVal bLoadStructure As Boolean = False)

            Dim oPageElmt As XmlElement
            Dim sProcessInfo As String = ""
            Dim sLayout As String = "default"

            Try

                If moPageXML.DocumentElement Is Nothing Then
                    moPageXML.CreateXmlDeclaration("1.0", "UTF-8", "yes")
                    oPageElmt = moPageXML.CreateElement("Page")
                    moPageXML.AppendChild(oPageElmt)
                    oWeb.GetRequestVariablesXml(oPageElmt)
                    oWeb.GetSettingsXml(oPageElmt)
                Else
                    oPageElmt = moPageXML.DocumentElement
                End If

                If Not myWeb.mnUserId = 0 Then
                    'oPageElmt.AppendChild(oWeb.GetUserXML())
                    If bLoadStructure Then
                        'oPageElmt.AppendChild(oWeb.GetStructureXML())
                        ' Ensure that the structure is always called from the root level, and not from any manipulation of the top level,
                        ' such as AuthenticatedRootPageId
                        myWeb.GetStructureXML("Site", -1, mnAdminTopLevel)
                    End If
                End If

                oPageElmt.SetAttribute("layout", sLayout)
                oPageElmt.SetAttribute("id", myWeb.mnPageId)
                oPageElmt.SetAttribute("cssFramework", moConfig("cssFramework"))
                oPageElmt.SetAttribute("userIntegrations", gbUserIntegrations.ToString.ToLower)

                'not sure if we need this block
                If myWeb.moRequest("artid") <> "" Then
                    myWeb.mnArtId = myWeb.moRequest("artid")
                Else
                    myWeb.mnArtId = myWeb.moRequest("id")
                End If

                If myWeb.mnArtId > 0 Then
                    oPageElmt.SetAttribute("artid", myWeb.mnArtId)
                End If

            Catch ex As Exception

                returnException(mcModuleName, "buildPageXML", ex, "", sProcessInfo, gbDebug)

            End Try

        End Sub

        Public Overridable Function GetAdminMenu() As XmlElement

            Dim oMenuRoot As XmlElement = Nothing
            'Dim oMenu As XmlElement
            'Dim oMenuMod As XmlElement
            'Dim oMenuItem As XmlElement
            'Dim oOldMenuItem As XmlElement
            Dim filePath As String = ""
            Dim oMenuElmt As XmlElement

            'old
            'Dim oElmt As XmlElement
            Dim oElmt1 As XmlElement
            Dim oElmt2 As XmlElement
            Dim oElmt3 As XmlElement
            Dim oElmt4 As XmlElement

            'Dim ewLastCmd As String
            Dim oPageElmt As XmlElement

            Dim sProcessInfo As String = ""
            Try
                'TS - Removed as seems to cause an issue with popup image selects etc.
                'seems the wrong place for this kind of cmd.
                'ewLastCmd = myWeb.moSession("ewCmd")
                'If ewLastCmd = "NormalMail" Then mcEwCmd = ewLastCmd

                If moPageXML.DocumentElement Is Nothing Then
                    moPageXML.CreateXmlDeclaration("1.0", "UTF-8", "yes")
                    oPageElmt = moPageXML.CreateElement("Page")
                    moPageXML.AppendChild(oPageElmt)
                    myWeb.GetRequestVariablesXml(oPageElmt)
                    myWeb.GetSettingsXml(oPageElmt)
                Else
                    oPageElmt = moPageXML.DocumentElement
                End If


                Dim aFolders As New ArrayList()
                aFolders.Add("")

                For Each folder As String In myWeb.maCommonFolders
                    aFolders.Add(folder)
                Next

                ' Look for a base menu - first locally, then from common alternative folders
                For Each folder As String In New ReverseIterator(aFolders)
                    filePath = folder.TrimEnd("/\".ToCharArray) & "/Admin/AdminMenu.xml"
                    If oMenuRoot Is Nothing Then
                        oMenuRoot = Tools.Xml.loadElement(myWeb.goServer.MapPath(filePath), moPageXML)
                    Else
                        Dim oTempMenuRoot As XmlElement
                        Dim oElmt As XmlElement
                        oTempMenuRoot = Tools.Xml.loadElement(myWeb.goServer.MapPath(filePath), moPageXML)
                        'add any new nodes
                        If Not oTempMenuRoot Is Nothing Then
                            For Each oElmt In oTempMenuRoot.SelectNodes("descendant-or-self::MenuItem[not(ancestor-or-self::MenuItem[@replacePath])]")

                                If oMenuRoot.SelectSingleNode("descendant-or-self::MenuItem[@cmd ='" & oElmt.GetAttribute("cmd") & "' ]") Is Nothing Then
                                    'we can't find it lets add it
                                    Dim oParentElmt As XmlElement = oElmt.ParentNode
                                    If Not (oParentElmt.GetAttribute("cmd") = "" And oMenuRoot.SelectSingleNode("descendant-or-self::MenuItem[@cmd ='" & oParentElmt.GetAttribute("cmd") & "' ]") Is Nothing) Then
                                        oMenuRoot.SelectSingleNode("descendant-or-self::MenuItem[@cmd ='" & oParentElmt.GetAttribute("cmd") & "' ]").AppendChild(oElmt)
                                    End If
                                End If
                            Next

                            For Each oElmt In oTempMenuRoot.SelectNodes("descendant-or-self::MenuItem[@replacePath!='']")

                                Dim oParentElmt As XmlElement = oElmt.ParentNode
                                Dim oRepElmt As XmlElement = oMenuRoot.SelectSingleNode("descendant-or-self::" & oElmt.GetAttribute("replacePath"))
                                oRepElmt.ParentNode.ReplaceChild(oElmt, oRepElmt)

                            Next
                        End If
                    End If
                    'If oMenuRoot IsNot Nothing Then Exit For
                Next

                If oMenuRoot Is Nothing Then
                    'build the old way 
                    oMenuRoot = moPageXML.CreateElement("AdminMenu")
                    oElmt1 = appendMenuItem(oMenuRoot, "Admin Home", "AdmHome")
                    oElmt2 = appendMenuItem(oElmt1, "Content", "Content")
                    oElmt3 = appendMenuItem(oElmt2, "By Page", "ByPage")
                    If mcEwCmd = "ByPage" Or mcEwCmd = "Normal" Or mcEwCmd = "AddContent" Or mcEwCmd = "AddModule" Or mcEwCmd = "ContentVersions" Or mcEwCmd = "RollbackContent" Or mcEwCmd = "RelateSearch" Or mcEwCmd = "EditContent" Or mcEwCmd = "Advanced" Or mcEwCmd = "EditPage" Or mcEwCmd = "EditPageLayout" Or mcEwCmd = "EditPagePermissions" Or mcEwCmd = "EditPageRights" Or mcEwCmd = "LocateContent" Or mcEwCmd = "LocateSearch" Or mcEwCmd = "MoveContent" Or mcEwCmd = "admin" Or mcEwCmd = "AddScheduledItem" Then
                        oElmt4 = appendMenuItem(oElmt3, "Normal Mode", "Normal", myWeb.mnPageId)
                        appendMenuItem(oElmt4, "Edit Content", "EditContent", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Add Module", "AddModule", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Add Content", "AddContent", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Move Content", "MoveContent", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Locate Content", "LocateContent", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Relate Search", "RelateSearch", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Locate Search", "LocateSearch", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Content Versions", "ContentVersions", myWeb.mnPageId, False)
                        appendMenuItem(oElmt4, "Rollback Content", "RollbackContent", myWeb.mnPageId, False)
                        appendMenuItem(oElmt3, "Advanced Mode", "Advanced", myWeb.mnPageId)
                        appendMenuItem(oElmt3, "Page Settings", "EditPage", myWeb.mnPageId)
                        appendMenuItem(oElmt3, "Page Layout", "EditPageLayout", myWeb.mnPageId)
                        If moConfig("Membership") = "on" Then
                            appendMenuItem(oElmt3, "Permissions", "EditPagePermissions", myWeb.mnPageId)
                            appendMenuItem(oElmt3, "Rights", "EditPageRights", myWeb.mnPageId)
                        End If
                        appendMenuItem(oElmt3, "Preview", "PreviewOn", myWeb.mnPageId)
                    End If
                    oElmt3 = appendMenuItem(oElmt2, "Edit Menu", "EditStructure")
                    appendMenuItem(oElmt3, "Move Page", "MovePage", myWeb.mnPageId, False)
                    appendMenuItem(oElmt3, "Add Page", "AddPage", myWeb.mnPageId, False)
                    oElmt3 = appendMenuItem(oElmt2, "Resource Library", "ImageLib")
                    If mcEwCmd = "Library" Or mcEwCmd = "ImageLib" Or mcEwCmd = "DocsLib" Or mcEwCmd = "MediaLib" Then
                        appendMenuItem(oElmt3, "Image Library", "ImageLib")
                        appendMenuItem(oElmt3, "Document Library", "DocsLib")
                        appendMenuItem(oElmt3, "Media Library", "MediaLib")
                    End If

                    oElmt3 = appendMenuItem(oElmt2, "Web Settings", "WebSettings")
                    appendMenuItem(oElmt3, "Select Skin", "SelectSkin")
                    appendMenuItem(oElmt3, "Scheduled Items", "ScheduledItems")
                    appendMenuItem(oElmt3, "System Pages", "SystemPages")
                    appendMenuItem(oElmt3, "System Pages", "ViewSystemPages", , False)
                    'ViewSystemPages
                    If moConfig("SiteSearch") = "on" Then
                        appendMenuItem(oElmt3, "Index Site", "SiteIndex")
                    End If
                    ' oElmt3 = appendMenuItem(oElmt2, "By Type", "ByType")
                    ' If mcEwCmd = "ByType" Then
                    '    appendMenuItem(oElmt3, "ListAll", "List All Quizzes")
                    '    appendMenuItem(oElmt3, "AddContent", "Add New Quiz")
                    'End If

                    If moConfig("VersionControl") = "on" Then
                        oElmt3 = appendMenuItem(oElmt2, "Awaiting Approval", "AwaitingApproval")
                    End If

                    If moConfig("Import") = "on" Then
                        oElmt3 = appendMenuItem(oElmt2, "Import Files", "FileImport")
                    End If

                    ' RJP 21 Nov 2012 Added ImpersonateUser to list
                    If moConfig("Membership") = "on" Then
                        oElmt2 = appendMenuItem(oElmt1, "Membership", "ListGroups")
                        If mcEwCmd = "ListUsers" Or mcEwCmd = "EditDirItem" Or mcEwCmd = "ListCompanies" Or mcEwCmd = "ListGroups" Or mcEwCmd = "ListRoles" Or mcEwCmd = "ListMailingLists" Or mcEwCmd = "Permissions" Or mcEwCmd = "DirPermissions" _
                        Or mcEwCmd = "Subscriptions" Or mcEwCmd = "AddSubscriptionGroup" Or mcEwCmd = "EditSubscriptionGroup" Or mcEwCmd = "AddSubscription" Or mcEwCmd = "EditSubscription" Or mcEwCmd = "MoveSubscription" Or mcEwCmd = "LocateSubscription" Or mcEwCmd = "UpSubscription" Or mcEwCmd = "DownSubscription" _
                        Or mcEwCmd = "MemberActivity" Or mcEwCmd = "MemberCodes" Or mcEwCmd = "DeleteDirItem" Or mcEwCmd = "DirPermissions" Or mcEwCmd = "ResetUserPwd" Or mcEwCmd = "MaintainRelations" Or mcEwCmd = "ListUserContacts" Or mcEwCmd = "AddUserContact" Or mcEwCmd = "EditUserContact" Or mcEwCmd = "ImpersonateUser" Then
                            appendMenuItem(oElmt2, "Groups", "ListGroups")
                            appendMenuItem(oElmt2, "All Users", "ListUsers")
                            appendMenuItem(oElmt2, "Roles", "ListRoles")
                            appendMenuItem(oElmt2, "Edit Item", "EditDirItem", , False)
                            ' RJP 21 Nov 2012 Added ImpersonateUser to list
                            appendMenuItem(oElmt2, "Impersonate User", "ImpersonateUser", , False)
                            appendMenuItem(oElmt2, "ResetPwd", "ResetUserPwd", , False)
                            appendMenuItem(oElmt2, "DirPermissions", "DirPermissions", , False)
                            appendMenuItem(oElmt2, "Maintain Relations", "MaintainRelations", , False)
                            appendMenuItem(oElmt2, "ListUserContacts", "ListUserContacts", , False)
                            appendMenuItem(oElmt2, "AddUserContact", "AddUserContact", , False)
                            appendMenuItem(oElmt2, "EditUserContact", "EditUserContact", , False)
                            'appendMenuItem(oElmt2, "Companies", "ListCompanies")
                            appendMenuItem(oElmt2, "Access Permission", "Permissions")
                            If moConfig("Subscriptions") = "on" Then
                                oElmt3 = appendMenuItem(oElmt2, "Subscriptions", "Subscriptions")
                            End If
                            If moConfig("ActivityReporting") = "on" Then
                                appendMenuItem(oElmt2, "Activity Reporting", "MemberActivity")
                            End If
                            If moConfig("MemberCodes") = "on" Then
                                appendMenuItem(oElmt2, "Member Codes", "MemberCodes")
                            End If
                        End If

                        If moConfig("MailingList") = "on" Then
                            oElmt2 = appendMenuItem(oElmt1, "Mailing List", "MailingList")
                            If mcEwCmd = "MailingList" Or mcEwCmd = "NewMail" Or mcEwCmd = "AdvancedMail" Or mcEwCmd = "NormalMail" Or mcEwCmd = "MailHistory" Or mcEwCmd = "ProcessMailbox" Or mcEwCmd = "MailOptOut" Or mcEwCmd = "PreviewMail" Or mcEwCmd = "AddContentMail" Or mcEwCmd = "NormalMail" Or mcEwCmd = "AdvancedMail" Or mcEwCmd = "EditMail" Or mcEwCmd = "EditMailLayout" Or mcEwCmd = "PreviewMail" Or mcEwCmd = "SendMail" Or mcEwCmd = "AddContentMail" Then
                                oElmt3 = appendMenuItem(oElmt2, "Mail Items", "MailingList")
                                If mcEwCmd = "NormalMail" Or mcEwCmd = "AdvancedMail" Or mcEwCmd = "EditMail" Or mcEwCmd = "EditMailLayout" Or mcEwCmd = "PreviewMail" Or mcEwCmd = "SendMail" Or mcEwCmd = "AddContentMail" Then
                                    appendMenuItem(oElmt3, "Normal Mode", "NormalMail", myWeb.mnPageId)
                                    appendMenuItem(oElmt3, "Advanced Mode", "AdvancedMail", myWeb.mnPageId)
                                    appendMenuItem(oElmt3, "Mail Settings", "EditMail", myWeb.mnPageId)
                                    appendMenuItem(oElmt3, "Mail Layout", "EditMailLayout", myWeb.mnPageId)
                                    appendMenuItem(oElmt3, "Send Preview", "PreviewMail", myWeb.mnPageId)
                                    appendMenuItem(oElmt3, "Send Mail", "SendMail", myWeb.mnPageId)
                                End If
                                appendMenuItem(oElmt2, "History", "MailHistory")
                                'appendMenuItem(oElmt2, "ProcessMailbox", "ProcessMailbox")
                                appendMenuItem(oElmt2, "Opt-Out", "MailOptOut")
                            End If
                        End If
                    Else
                        oElmt2 = appendMenuItem(oElmt1, "Membership", "ListGroups")
                        If mcEwCmd = "ListUsers" Or mcEwCmd = "EditDirItem" Or mcEwCmd = "ListCompanies" Or mcEwCmd = "ListGroups" Or mcEwCmd = "ListRoles" Or mcEwCmd = "ListMailingLists" Or mcEwCmd = "Permissions" Or mcEwCmd = "DirPermissions" Then
                            appendMenuItem(oElmt2, "All Users", "ListUsers")
                            appendMenuItem(oElmt2, "Roles", "ListRoles")
                        End If
                    End If
                    If moConfig("Cart") = "on" Or moConfig("Quote") = "on" Then
                        oElmt2 = appendMenuItem(oElmt1, "Ecommerce", "Ecommerce")
                        If moConfig("Cart") = "on" Then
                            oElmt3 = appendMenuItem(oElmt2, "Orders", "Orders")
                            If mcEwCmd = "Orders" Or mcEwCmd = "OrdersShipped" Or mcEwCmd = "OrdersAwaitingPayment" Or mcEwCmd = "OrdersFailed" Or mcEwCmd = "OrdersDeposit" Or mcEwCmd = "OrdersHistory" Then
                                appendMenuItem(oElmt3, "New Sales", "Orders")
                                appendMenuItem(oElmt3, "Awaiting Payment", "OrdersAwaitingPayment")
                                appendMenuItem(oElmt3, "Shipped", "OrdersShipped")
                                appendMenuItem(oElmt3, "Failed Transactions", "OrdersFailed")
                                appendMenuItem(oElmt3, "Deposit Paid", "OrdersDeposit")
                                appendMenuItem(oElmt3, "History", "OrdersHistory")
                            End If
                        End If
                        If moConfig("Quote") = "on" Then
                            oElmt3 = appendMenuItem(oElmt2, "Quotes", "Quotes")
                            If mcEwCmd = "Quotes" Or mcEwCmd = "QuotesFailed" Or mcEwCmd = "QuotesDeposit" Or mcEwCmd = "QuotesHistory" Then
                                appendMenuItem(oElmt3, "New Sales", "Quotes")
                                appendMenuItem(oElmt3, "Failed Transactions", "QuotesFailed")
                                appendMenuItem(oElmt3, "Deposit Paid", "QuotesDeposit")
                                appendMenuItem(oElmt3, "History", "QuotesHistory")
                            End If
                        End If


                        oElmt3 = appendMenuItem(oElmt2, "Shipping Locations", "ShippingLocations")
                        oElmt3 = appendMenuItem(oElmt2, "Delivery Methods", "DeliveryMethods")


                        oElmt3 = appendMenuItem(oElmt2, "Discounts", "Discounts")
                        If mcEwCmd = "Discounts" Or mcEwCmd = "ProductGroups" Or mcEwCmd = "DiscountRules" _
                        Or mcEwCmd = "DiscountGroupRelations" Or mcEwCmd = "EditProductGroups" Or mcEwCmd = "AddProductGroupsProduct" _
                        Or mcEwCmd = "AddDiscountRules" Or mcEwCmd = "EditDiscountRules" Or mcEwCmd = "ApplyDirDiscountRules" _
                        Or mcEwCmd = "ApplyGrpDiscountRules" Then
                            appendMenuItem(oElmt3, "Product Groups", "ProductGroups")
                            appendMenuItem(oElmt3, "Discount Rules", "DiscountRules")
                            'These are all hidden menu items
                            'the menu builder looks for cmdn to match to display the menu
                            appendMenuItem(oElmt3, "Discounts", "Discounts", , False)
                            appendMenuItem(oElmt3, "ProductGroups", "ProductGroups", , False)
                            appendMenuItem(oElmt3, "DiscountRules", "DiscountRules", , False)
                            appendMenuItem(oElmt3, "DiscountGroupRelations", "DiscountGroupRelations", , False)
                            appendMenuItem(oElmt3, "EditProductGroups", "EditProductGroups", , False)
                            appendMenuItem(oElmt3, "AddProductGroupsProduct", "AddProductGroupsProduct", , False)
                            appendMenuItem(oElmt3, "AddDiscountRules", "AddDiscountRules", , False)
                            appendMenuItem(oElmt3, "EditDiscountRules", "EditDiscountRules", , False)
                            appendMenuItem(oElmt3, "ApplyDirDiscountRules", "ApplyDirDiscountRules", , False)
                            appendMenuItem(oElmt3, "ApplyGrpDiscountRules", "ApplyGrpDiscountRules", , False)
                            appendMenuItem(oElmt3, "AddProductGroups", "AddProductGroups", , False)
                        End If

                        'oElmt3 = appendMenuItem(oElmt2, "Reports", "CartReports")
                        'oElmt3 = appendMenuItem(oElmt2, "Settings", "CartSettings")
                        'If mcEwCmd = "CartSettings" Or mcEwCmd = "PaymentProviders" Or mcEwCmd = "CartTandC" Or mcEwCmd = "ProductCategories" Or mcEwCmd = "CartDiscounts" Then
                        '    appendMenuItem(oElmt3, "Payment Providers", "PaymentProviders")
                        '    appendMenuItem(oElmt3, "Terms & Conditions", "CartTandC")
                        '    appendMenuItem(oElmt3, "Product Categories", "ProductCategories")
                        '    appendMenuItem(oElmt3, "Discounts", "CartDiscounts")
                        'End If

                        'Cart Settings
                        oElmt3 = appendMenuItem(oElmt2, "Settings", "CartSettings")
                        If mcEwCmd = "CartSettings" Or mcEwCmd = "PaymentProviders" Or mcEwCmd = "editProvider" Then
                            appendMenuItem(oElmt3, "General Settings", "CartSettings")
                            appendMenuItem(oElmt3, "Payment Providers", "PaymentProviders")
                        End If

                        If moConfig("Sync") = "on" Then
                            oElmt3 = appendMenuItem(oElmt2, "Synchronisation", "Sync")
                        End If
                    End If

                    'Cart Reports
                    oElmt3 = appendMenuItem(oElmt2, "Reports", "CartReportsMain")
                    appendMenuItem(oElmt3, "Order Download", "CartDownload")
                    appendMenuItem(oElmt3, "Sales by Product", "CartReports")
                    appendMenuItem(oElmt3, "Sales by Page", "CartActivityDrilldown")
                    appendMenuItem(oElmt3, "Sales by Period", "CartActivityPeriod")

                    oElmt2 = appendMenuItem(oElmt1, "Reports", "Reports")
                    'oElmt3 = appendMenuItem(oElmt2, "By Company", "RptCompanies")
                    'oElmt3 = appendMenuItem(oElmt2, "Courses", "RptCourses")
                    'oElmt3 = appendMenuItem(oElmt2, "All Certificates", "RptCertificates")
                    'oElmt3 = appendMenuItem(oElmt2, "All Exam Activity", "RptExamActivity")
                    'oElmt3 = appendMenuItem(oElmt2, "All Page Activity", "RptPageActivity")
                    'oElmt3 = appendMenuItem(oElmt2, "Company Activity", "RptCompActivity")

                End If

                'Add any options in Manifests


                'Remove non-licenced features
                For Each oMenuElmt In oMenuRoot.SelectNodes("descendant-or-self::MenuItem[@feature!='']")
                    Dim cFeature As String = oMenuElmt.GetAttribute("feature")
                    If Not myWeb.Features.ContainsKey(cFeature) Then
                        oMenuElmt.ParentNode.RemoveChild(oMenuElmt)
                    End If
                Next

                'Remove any options by role

                SupplimentalAdminMenu(oMenuRoot)

                Dim moMailConfig As System.Collections.Specialized.NameValueCollection = WebConfigurationManager.GetWebApplicationSection("eonic/mailinglist")
                Dim sMessagingProvider As String = ""
                If Not moMailConfig Is Nothing Then
                    sMessagingProvider = moMailConfig("MessagingProvider")
                End If
                Dim oMessaging As New Eonic.Providers.Messaging.BaseProvider(myWeb, sMessagingProvider)
                oMessaging.AdminProcess.MailingListAdminMenu(oMenuRoot)
                oMessaging = Nothing

                ' If this is a cloned page, then remove certain options under By Page
                If gbClone _
                    AndAlso Not (moPageXML.DocumentElement.SelectSingleNode("//MenuItem[@id = /Page/@id and (@clone > 0 or (@cloneparent='" & myWeb.mnCloneContextPageId & "' and @cloneparent > 0 ))]") Is Nothing) Then

                    Dim oByPage As XmlElement
                    If Tools.Xml.NodeState(oMenuRoot, "//MenuItem[@cmd='ByPage']", , , , oByPage) <> Tools.Xml.XmlNodeState.NotInstantiated Then
                        For Each oMenuElmt In oByPage.SelectNodes("MenuItem[not(@cmd='Normal' or @cmd='Advanced')]")
                            If Not ( _
                                        (oMenuElmt.GetAttribute("cmd") = "EditPage" _
                                            Or oMenuElmt.GetAttribute("cmd") = "EditPagePermissions" _
                                        ) And myWeb.mbIsClonePage) _
                                    Then oMenuElmt.SetAttribute("display", "false")
                        Next
                        '<MenuItem name="Page Settings" cmd="EditPage" pgid="1" display="true"/>
                        oMenuElmt = addElement(oByPage, "MenuItem")
                        oMenuElmt.SetAttribute("name", "Go to master page")
                        oMenuElmt.SetAttribute("cmd", "GoToClone")
                        oMenuElmt.SetAttribute("display", "true")
                    End If
                End If
                If oPageElmt.SelectSingleNode("AdminMenu") Is Nothing Then
                    oPageElmt.AppendChild(oMenuRoot)
                Else
                    oPageElmt.ReplaceChild(oMenuRoot, oPageElmt.SelectSingleNode("AdminMenu"))
                End If

                Return oMenuRoot
            Catch ex As Exception
                returnException(mcModuleName, "GetAdminMenu", ex, "", sProcessInfo, gbDebug)
                Return Nothing
            End Try

        End Function

        Private Sub GetPreviewMenu()
            Dim oElmt As XmlElement
            Dim oElmt1 As XmlElement
            Dim oElmt2 As XmlElement
            Dim sProcessInfo As String = ""
            Try
                oElmt = moPageXML.CreateElement("PreviewMenu")

                appendMenuItem(oElmt, "Return to Admin", "PreviewOff", myWeb.mnPageId, True)
                oElmt1 = appendMenuItem(oElmt, "Date", "", myWeb.mnPageId, True)
                oElmt2 = appendMenuItem(oElmt, "User", "", myWeb.mnPageId, True)
                If myWeb.moSession("PreviewUser") = 0 Then
                    oElmt1.SetAttribute("username", "Anonymous")
                Else
                    oElmt1.SetAttribute("username", myWeb.moDbHelper.getUserXMLById(myWeb.moSession("PreviewUSer")).GetAttribute("name"))
                End If
                oElmt2.SetAttribute("date", IIf(IsDate(myWeb.moSession("PreviewDate")), myWeb.moSession("PreviewDate"), Now.Date))

                'also need to add an xform for the group and the date


                moPageXML.DocumentElement.AppendChild(oElmt)

            Catch ex As Exception
                returnException(mcModuleName, "GetpreviewMenu", ex, "", sProcessInfo, gbDebug)
            End Try

        End Sub

        Public Function appendMenuItem(ByRef oRootElmt As XmlElement, ByVal cName As String, ByVal cCmd As String, Optional ByVal pgid As Long = 0, Optional ByVal display As Boolean = True) As XmlElement

            Dim oElmt As XmlElement
            Dim sProcessInfo As String = ""
            Try

                oElmt = moPageXML.CreateElement("MenuItem")
                oElmt.SetAttribute("name", cName)
                oElmt.SetAttribute("cmd", cCmd)
                If pgid <> 0 Then
                    oElmt.SetAttribute("pgid", pgid)
                End If
                If display Then
                    oElmt.SetAttribute("display", "true")
                Else
                    oElmt.SetAttribute("display", "false")
                End If
                oRootElmt.AppendChild(oElmt)

                Return oElmt

            Catch ex As Exception
                returnException(mcModuleName, "appendMenuItem", ex, "", sProcessInfo, gbDebug)
                Return Nothing
            End Try
        End Function


        Private Sub LibProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String, ByVal LibType As fsHelper.LibraryType)
            Dim sProcessInfo As String = ""
            Try

                Dim oFsh As fsHelper = New fsHelper
                oFsh.initialiseVariables(LibType)
                oFsh.moPageXML = moPageXML

                If myWeb.moRequest("targetForm") <> "" Then myWeb.moSession("targetForm") = myWeb.moRequest("targetForm")
                Dim sTargetForm As String = myWeb.moSession("targetForm")

                If myWeb.moRequest("targetField") <> "" Then myWeb.moSession("targetField") = myWeb.moRequest("targetField")
                Dim sTargetField As String = myWeb.moSession("targetField")

                If myWeb.moRequest("targetClass") <> "" Then myWeb.moSession("targetClass") = myWeb.moRequest("targetClass")
                Dim sTargetClass As String = myWeb.moSession("targetClass")

                Dim bShowTree As Boolean = False
                Dim sFolder As String = myWeb.goServer.UrlDecode(myWeb.moRequest("fld"))
                Dim sFile As String = myWeb.moRequest("file")

                Select Case myWeb.moRequest("ewCmd2")
                    Case "addFolder"
                        oPageDetail.AppendChild(moAdXfm.xFrmAddFolder(sFolder, LibType))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            bShowTree = True
                        End If
                    Case "upload"
                        oPageDetail.AppendChild(moAdXfm.xFrmUpload(sFolder, LibType))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            bShowTree = True
                        End If
                    Case "uploads"
                        oPageDetail.AppendChild(moAdXfm.xFrmMultiUpload(sFolder, LibType))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            bShowTree = True
                        End If
                    Case "deleteFolder"
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteFolder(sFolder, LibType))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            bShowTree = True
                        End If
                    Case "deleteFile"
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteFile(sFolder, sFile, LibType))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            bShowTree = True
                        End If
                    Case "moveFile"

                    Case "pickImage"
                        Dim imagePath As String = IIf(sFolder.Replace("\", "/").EndsWith("/"), sFolder.Replace("\", "/") & sFile, sFolder & "/" & sFile)
                        oPageDetail.AppendChild(moAdXfm.xFrmPickImage(imagePath, sTargetForm, sTargetField, sTargetClass))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            'close window / js
                        End If
                    Case "pickDocument"
                        'all done in js
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            'close window / js
                        End If
                    Case "editImage"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditImage(myWeb.moRequest("imgHtml"), sTargetForm, sTargetField, ""))
                        If moAdXfm.valid = False Then
                            sAdminLayout = "AdminXForm"
                        Else
                            'close window / js
                        End If
                    Case "FolderSettings"
                    Case Else
                        bShowTree = True
                End Select

                If bShowTree = True Then
                    oPageDetail.AppendChild(oFsh.getDirectoryTreeXml(LibType, sFolder))
                End If
                oFsh = Nothing

            Catch ex As Exception
                returnException(mcModuleName, "LibProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub OrderProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String, ByVal cSchemaName As String)
            Dim sProcessInfo As String = ""

            Try

                If myWeb.moRequest("ewCmd").Contains("Order") Then
                    Dim oCart As New Cart(myWeb)

                    Select Case myWeb.moRequest("ewCmd2")
                        Case "Display"

                            Dim nStatus As Long
                            nStatus = myWeb.moDbHelper.getObjectStatus(dbHelper.objectTypes.CartOrder, myWeb.moRequest("id"))

                            oPageDetail.AppendChild(moAdXfm.xFrmUpdateOrder(myWeb.moRequest("id"), cSchemaName))

                            Dim forceRefresh As Boolean = False

                            If myWeb.moRequest("nStatus") = 9 Then
                                forceRefresh = True
                            End If
                            oCart.ListOrders(myWeb.moRequest("id"), True, , oPageDetail, forceRefresh)

                            ':TODO Behaviour to manage resending recipts.
                            If moAdXfm.isSubmitted And moAdXfm.valid Then
                                If nStatus <> myWeb.moRequest("nStatus") And myWeb.moRequest("nStatus") = Cart.cartProcess.Complete Then
                                    oCart.mnCartId = myWeb.moRequest("id")
                                    oCart.addDateAndRef(oPageDetail.LastChild.FirstChild)
                                    oCart.emailReceipts(oPageDetail.LastChild)
                                End If

                            End If


                        Case "ResendReceipt"

                            oCart.ListOrders(myWeb.moRequest("id"), True, , oPageDetail)


                        Case Else
                            Select Case myWeb.moRequest("ewCmd")
                                Case "Orders"
                                    oCart.ListOrders(0, True, Cart.cartProcess.Complete, oPageDetail)
                                Case "OrdersAwaitingPayment"
                                    oCart.ListOrders(0, True, Cart.cartProcess.AwaitingPayment, oPageDetail)
                                Case "OrdersShipped"
                                    oCart.ListOrders(0, True, Cart.cartProcess.Shipped, oPageDetail)
                                Case "OrdersRefunded"
                                    oCart.ListOrders(0, True, Cart.cartProcess.Refunded, oPageDetail)
                                Case "OrdersAbandoned"
                                    oCart.ListOrders(0, True, Cart.cartProcess.Abandoned, oPageDetail)
                                Case "OrdersFailed"
                                    oCart.ListOrders(0, True, Cart.cartProcess.PassForPayment, oPageDetail)
                                Case "OrdersDeposit"
                                    oCart.ListOrders(0, True, Cart.cartProcess.DepositPaid, oPageDetail)
                                Case "OrdersHistory"
                                    oCart.ListOrders(0, True, , oPageDetail)
                            End Select
                    End Select
                    sAdminLayout = cSchemaName & "s"
                ElseIf myWeb.moRequest("ewCmd").Contains("Quote") Then
                    Dim oQuote As New Quote(myWeb)

                    Select Case myWeb.moRequest("ewCmd2")

                        Case "Display"
                            oPageDetail.AppendChild(moAdXfm.xFrmUpdateOrder(myWeb.moRequest("id"), cSchemaName))
                            oQuote.ListOrders(myWeb.moRequest("id"), True, , oPageDetail)


                        Case Else
                            Select Case myWeb.moRequest("ewCmd")
                                Case "Quotes"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.Complete, oPageDetail)
                                Case "QuotesShipped"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.Shipped, oPageDetail)
                                Case "QuotesRefunded"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.Refunded, oPageDetail)
                                Case "QuotesAbandoned"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.Abandoned, oPageDetail)
                                Case "QuotesFailed"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.PassForPayment, oPageDetail)
                                Case "QuotesDeposit"
                                    oQuote.ListOrders(0, True, Cart.cartProcess.DepositPaid, oPageDetail)
                                Case "QuotesHistory"
                                    oQuote.ListOrders(0, True, , oPageDetail)
                            End Select
                    End Select
                    sAdminLayout = cSchemaName & "s"
                ElseIf myWeb.moRequest("ewCmd") = "CartActivity" Or myWeb.moRequest("ewCmd") = "CartReports" Then
                    Dim oCart As New Cart(myWeb)
                    oPageDetail.AppendChild(moAdXfm.xFrmCartActivity)
                    If moAdXfm.valid Then
                        oPageDetail.AppendChild(oCart.CartReports( _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("dBegin").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("dEnd").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("bSplit").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cProductType").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nProductId").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cCurrencySymbol").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus1").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus2").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cOrderType").InnerText _
                        ))
                    End If
                    sAdminLayout = "CartActivity"
                ElseIf myWeb.moRequest("ewCmd") = "CartActivityDrilldown" Then
                    Dim oCart As New Cart(myWeb)
                    oPageDetail.AppendChild(moAdXfm.xFrmCartActivityDrillDown)
                    If moAdXfm.valid Then

                        oPageDetail.AppendChild(oCart.CartReportsDrilldown( _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cGrouping").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nYear").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nMonth").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nDay").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cCurrencySymbol").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus1").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus2").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cOrderType").InnerText _
                        ))
                    End If
                    sAdminLayout = "CartActivityDrilldown"
                ElseIf myWeb.moRequest("ewCmd") = "CartActivityPeriod" Then
                    Dim oCart As New Cart(myWeb)
                    oPageDetail.AppendChild(moAdXfm.xFrmCartActivityPeriod)
                    If moAdXfm.valid Then
                        oPageDetail.AppendChild(oCart.CartReportsPeriod( _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cGroup").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nYear").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nMonth").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nWeek").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cCurrencySymbol").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus1").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("nOrderStatus2").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cOrderType").InnerText _
                        ))
                    End If
                    sAdminLayout = "CartActivityPeriod"
                ElseIf myWeb.moRequest("ewCmd") = "CartDownload" Then
                    Dim oCart As New Cart(myWeb)
                    oPageDetail.AppendChild(moAdXfm.xFrmCartOrderDownloads)
                    If moAdXfm.valid Then
                        oPageDetail.AppendChild(oCart.CartReportsDownload( _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("dBegin").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("dEnd").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cCurrencySymbol").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cOrderType").InnerText, _
                        moAdXfm.Instance.FirstChild.SelectSingleNode("cOrderStage").InnerText _
                        ))
                    End If
                    sAdminLayout = "CartDownload"

                ElseIf myWeb.moRequest("ewCmd") = "Ecommerce" Then
                    Dim oCart As New Cart(myWeb)
                    oPageDetail.AppendChild(oCart.CartOverview)
                End If

            Catch ex As Exception
                returnException(mcModuleName, "OrderProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub ShippingLocationsProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim oCart As Cart

            Try
                oCart = New Cart(myWeb)

                Select Case myWeb.moRequest("ewCmd2")
                    Case "edit"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditShippingLocation(myWeb.moRequest("id"), myWeb.moRequest("parid")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                    Case "movehere"
                        myWeb.moDbHelper.moveShippingLocation(myWeb.moRequest("id"), myWeb.moRequest("parId"))
                    Case "delete"
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteShippingLocation(myWeb.moRequest("id")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                End Select
                If oPageDetail.InnerXml = "" Then
                    oCart.ListShippingLocations(oPageDetail)
                End If
                oCart.close()
                oCart = Nothing
            Catch ex As Exception
                returnException(mcModuleName, "ShippingLocationsProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub DeliveryMethodProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim oCart As Cart

            Try
                oCart = New Cart(myWeb)

                Select Case myWeb.moRequest("ewCmd2")
                    Case "edit"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditDeliveryMethod(myWeb.moRequest("id")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                    Case "locations"
                        If myWeb.moRequest("ewSubmit") <> "" Then
                            myWeb.moDbHelper.updateShippingLocations(myWeb.moRequest("nShpOptId"), myWeb.moRequest("aLocations"))
                        Else
                            oCart.ListShippingLocations(oPageDetail, CLng("0" & myWeb.moRequest("id")))
                            sAdminLayout = "DeliveryMethodLocations"
                        End If
                    Case "permissions"

                        sAdminLayout = "AdminXForm"
                        oPageDetail.AppendChild(moAdXfm.xFrmShippingDirRelations(myWeb.moRequest.QueryString("id"), ""))

                    Case "delete"
                        'xFrmDeleteDeliveryMethod
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteDeliveryMethod(myWeb.moRequest("id")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                            sAdminLayout = "DeliveryMethods"
                        End If
                End Select
                If oPageDetail.InnerXml = "" Then
                    oCart.ListDeliveryMethods(oPageDetail)
                End If
                oCart.close()
                oCart = Nothing
            Catch ex As Exception
                returnException(mcModuleName, "DeliveryMethodProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub CarriersProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim oCart As Cart

            Try
                oCart = New Cart(myWeb)

                Select Case myWeb.moRequest("ewCmd2")
                    Case "edit"
                        oPageDetail.AppendChild(moAdXfm.xFrmEditCarrier(myWeb.moRequest("id")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                    Case "delete"
                        'xFrmDeleteDeliveryMethod
                        oPageDetail.AppendChild(moAdXfm.xFrmDeleteCarrier(myWeb.moRequest("id")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                            sAdminLayout = "Carriers"
                        End If
                End Select
                If oPageDetail.InnerXml = "" Then
                    oCart.ListCarriers(oPageDetail)
                    sAdminLayout = "Carriers"
                End If
                oCart.close()
                oCart = Nothing
            Catch ex As Exception
                returnException(mcModuleName, "CarriersProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub PaymentProviderProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim oCart As Cart

            Try
                oCart = New Cart(myWeb)

                Select Case myWeb.moRequest("ewCmd2")
                    Case "edit", "add"
                        oPageDetail.AppendChild(moAdXfm.xFrmPaymentProvider(myWeb.moRequest("type")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                    Case "delete"
                        ':TODO delete payment provider xform
                        oPageDetail.AppendChild(moAdXfm.xFrmDeletePaymentProvider(myWeb.moRequest("type")))
                        If Not moAdXfm.valid Then
                            sAdminLayout = "AdminXForm"
                        Else
                            oPageDetail.RemoveAll()
                        End If
                End Select
                If oPageDetail.InnerXml = "" Then
                    oCart.ListPaymentProviders(oPageDetail)
                End If
                oCart.close()
                oCart = Nothing
            Catch ex As Exception
                returnException(mcModuleName, "DeliveryMethodProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub PollsProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim reportName As String = ""
            Dim contentId As Long = 0
            Dim logId As Long = 0
            Dim pollTitle As String = ""
            Dim votesDataset As DataSet
            Dim getVotesQuery As String
            Dim updateVoteQuery As String
            Try

                contentId = myWeb.GetRequestItemAsInteger("id")


                If contentId > 0 Then

                    logId = myWeb.GetRequestItemAsInteger("voteId")

                    ' Vote based commands
                    If logId > 0 Then
                        Select Case myWeb.moRequest("ewCmd2")
                            Case "delete"
                                ':TODO delete payment provider xform

                                If Not moAdXfm.valid Then
                                    sAdminLayout = "AdminXForm"
                                Else
                                    oPageDetail.RemoveAll()
                                End If
                            Case "hide"
                                updateVoteQuery = "UPDATE dbo.tblActivityLog " _
                                                    & "SET nActivityType=" & dbHelper.ActivityType.VoteExcluded & " " _
                                                    & "WHERE nActivityKey = " & logId
                                myWeb.moDbHelper.ExeProcessSql(updateVoteQuery)
                            Case "show"
                                updateVoteQuery = "UPDATE dbo.tblActivityLog " _
                                                    & "SET nActivityType=" & dbHelper.ActivityType.SubmitVote & " " _
                                                    & "WHERE nActivityKey = " & logId
                                myWeb.moDbHelper.ExeProcessSql(updateVoteQuery)
                        End Select
                    End If


                    ' Poll report name
                    Dim pollXmlString As String = myWeb.moDbHelper.getObjectInstance(dbHelper.objectTypes.Content, contentId)
                    If Not String.IsNullOrEmpty(pollXmlString) Then
                        Dim pollXml As New XmlDocument
                        pollXml.LoadXml(pollXmlString)
                        Tools.Xml.NodeState(pollXml.DocumentElement, "//Title", , , , , , pollTitle)
                    End If
                    reportName = "Poll Votes: " & pollTitle

                    ' List the votes

                    getVotesQuery = "SELECT CASE WHEN nActivityType=" & dbHelper.ActivityType.SubmitVote & " THEN 1 ELSE 0 END As Status, cContentSchemaName As contentSchema, nActivityKey As Vote_Id, cContentXmlBrief As Voted_For, dDateTime As Date_Voted, cActivityDetail As Voters_Email, cIPAddress As IP_Address " _
                            & "FROM dbo.tblActivityLog " _
                            & "LEFT JOIN dbo.tblContent ON nOtherId = nContentKey " _
                            & "WHERE nActivityType IN (" & dbHelper.ActivityType.SubmitVote & "," & dbHelper.ActivityType.VoteExcluded & ") AND nArtId = " & contentId

                    votesDataset = myWeb.moDbHelper.GetDataSet(getVotesQuery, "Vote", "GenericReport")
                    myWeb.moDbHelper.ReturnNullsEmpty(votesDataset)

                    If votesDataset.Tables.Count > 0 Then
                        votesDataset.Tables(0).Columns(1).ColumnMapping = MappingType.Attribute
                        votesDataset.Tables(0).Columns(2).ColumnMapping = MappingType.Attribute
                    End If

                    Dim votesReport As New XmlDataDocument(votesDataset)
                    votesDataset.EnforceConstraints = False

                    If votesReport.FirstChild IsNot Nothing Then


                        Dim reportElement As XmlElement = moPageXML.CreateElement("Content")
                        reportElement.SetAttribute("name", reportName)
                        reportElement.SetAttribute("type", "Report")

                        ' Convert the content into xml
                        For Each voteXml As XmlElement In votesReport.SelectNodes("//Voted_For")
                            Dim voteInnerXml As String = voteXml.InnerText
                            Try
                                voteXml.InnerXml = voteInnerXml
                            Catch ex As Exception
                                voteXml.InnerText = voteInnerXml
                            End Try
                        Next

                        reportElement.InnerXml = votesReport.InnerXml
                        oPageDetail.AppendChild(reportElement)

                    End If

                End If

                sAdminLayout = "ManagePollVotes"

            Catch ex As Exception
                returnException(mcModuleName, "PollsProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub ManageLookups(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            Dim reportName As String = "Manage Lookups"
            Dim contentId As Long = 0
            Dim lookupId As String = Nothing
            Dim sSql As String
            Dim lookupsDataset As DataSet


            Try

                If Not myWeb.moRequest("lookupId") = Nothing Then
                    lookupId = myWeb.moRequest("lookupId")
                End If

                Select Case myWeb.moRequest("ewCmd2")
                    Case "delete"
                        myWeb.moDbHelper.DeleteObject(dbHelper.objectTypes.Lookup, lookupId)

                        GoTo listItems
                    Case "hide"
                        sSql = "UPDATE dbo.tblLookup " _
                                            & "SET nActivityType=" & dbHelper.ActivityType.VoteExcluded & " " _
                                            & "WHERE nActivityKey = " & lookupId
                        myWeb.moDbHelper.ExeProcessSql(sSql)

                    Case "show"
                        sSql = "UPDATE dbo.tblLookup " _
                                            & "SET nActivityType=" & dbHelper.ActivityType.SubmitVote & " " _
                                            & "WHERE nActivityKey = " & lookupId
                        myWeb.moDbHelper.ExeProcessSql(sSql)
                    Case "MoveUp", "MoveDown", "MoveTop", "MoveBottom"

                        myWeb.moDbHelper.ReorderNode(dbHelper.objectTypes.Lookup, lookupId, myWeb.moRequest("ewCmd2"), "cLkpCategory")
                        lookupId = Nothing
                        GoTo listItems
                    Case Else
listItems:


                        If lookupId = Nothing Then
                            'list Lookup Lists
                            sSql = "select nLkpId as id, * from tblLookup order by cLkpCategory, nDisplayOrder"

                            lookupsDataset = myWeb.moDbHelper.GetDataSet(sSql, "Lookup", "Lookups")

                            myWeb.moDbHelper.addTableToDataSet(lookupsDataset, "select distinct cLkpCategory as Name from tblLookup", "Category")

                            myWeb.moDbHelper.ReturnNullsEmpty(lookupsDataset)

                            If lookupsDataset.Tables.Count > 0 Then

                                lookupsDataset.Tables(0).Columns("id").ColumnMapping = MappingType.Attribute
                                lookupsDataset.Tables(1).Columns("Name").ColumnMapping = MappingType.Attribute

                                ' lookupsDataset.Tables(0).Columns(2).ColumnMapping = MappingType.Attribute
                                lookupsDataset.Relations.Add("rel1", lookupsDataset.Tables(1).Columns("Name"), lookupsDataset.Tables(0).Columns("cLkpCategory"), False)
                                lookupsDataset.Relations("rel1").Nested = True

                                'lookupsDataset.Relations.Add("rel2", lookupsDataset.Tables(0).Columns("nLkpParent"), lookupsDataset.Tables(0).Columns("id"), False)
                                'lookupsDataset.Relations("rel2").Nested = True
                                lookupsDataset.EnforceConstraints = False

                            End If



                            Dim reportElement As XmlElement = moPageXML.CreateElement("Content")
                            reportElement.SetAttribute("name", reportName)
                            reportElement.SetAttribute("type", "Report")
                            reportElement.InnerXml = lookupsDataset.GetXml()
                            oPageDetail.AppendChild(reportElement)

                        Else
                            'lookupItem Xform
                            oPageDetail.AppendChild(moAdXfm.xFrmLookup(CLng(lookupId), myWeb.moRequest("Category"), myWeb.moRequest("parentId")))

                            If moAdXfm.valid Then
                                oPageDetail.InnerXml = ""
                                lookupId = Nothing
                                GoTo listItems
                            End If

                        End If


                End Select


                sAdminLayout = "ManageLookups"

            Catch ex As Exception
                returnException(mcModuleName, "PollsProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub ProductGroupsProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String, Optional ByVal nGroupID As Integer = 0)
            Dim sProcessInfo As String = ""
            sAdminLayout = "ProductGroups"
            Dim cSql As String
            Dim oDS As DataSet
            Try
                cSql = "Select * From tblCartProductCategories"
                oDS = myWeb.moDbHelper.GetDataSet(cSql, "ProductCategory", "ProductCategories")
                If oDS.Tables.Count = 1 Then
                    oDS.Tables("ProductCategory").Columns.Add("Count", GetType(Integer))
                    oDS.Tables("ProductCategory").Columns("Count").ColumnMapping = MappingType.Attribute
                    oDS.Tables("ProductCategory").Columns("nCatKey").ColumnMapping = MappingType.Attribute
                    oDS.Tables("ProductCategory").Columns("cCatSchemaName").ColumnMapping = MappingType.Attribute
                    oDS.Tables("ProductCategory").Columns("cCatForeignRef").ColumnMapping = MappingType.Attribute
                End If
                cSql = "SELECT c.nContentKey AS id, c.cContentForiegnRef AS ref, c.cContentName AS name, c.cContentSchemaName AS type, c.cContentXmlBrief AS content, tblCartCatProductRelations.nCatProductRelKey AS relid, tblCartCatProductRelations.nCatId AS catid FROM tblContent c INNER JOIN tblCartCatProductRelations ON c.nContentKey = tblCartCatProductRelations.nContentId " & _
                                    "WHERE (tblCartCatProductRelations.nCatId Is not Null)"
                myWeb.moDbHelper.addTableToDataSet(oDS, cSql, "Content")

                If oDS.Tables.Count = 2 Then

                    If oDS.Tables("Content").Columns.Contains("parID") Then
                        oDS.Tables("Content").Columns("parId").ColumnMapping = Data.MappingType.Attribute
                    End If
                    Dim oDC As DataColumn
                    For Each oDC In oDS.Tables("Content").Columns
                        If Not oDC.ColumnName = "content" Then oDC.ColumnMapping = MappingType.Attribute
                    Next
                    oDS.Tables("Content").Columns("content").ColumnMapping = Data.MappingType.SimpleContent

                    oDS.Relations.Add("CatCont", oDS.Tables("ProductCategory").Columns("nCatKey"), oDS.Tables("Content").Columns("catid"), False)

                    oDS.Relations("CatCont").Nested = True
                End If
                Dim oDr As DataRow
                For Each oDr In oDS.Tables("ProductCategory").Rows
                    oDr("Count") = oDr.GetChildRows("CatCont").Length
                    If Not oDr("nCatKey") = nGroupID Then
                        Dim oDr2 As DataRow
                        For Each oDr2 In oDr.GetChildRows("CatCont")
                            oDr2.Delete()
                        Next

                    End If
                Next
                Dim oElmt As XmlElement = oPageDetail.OwnerDocument.CreateElement("ProductCats")
                oElmt.InnerXml = Replace(Replace(oDS.GetXml, "&lt;", "<"), "&gt;", ">")
                oPageDetail.AppendChild(oElmt.FirstChild)


            Catch ex As Exception
                returnException(mcModuleName, "DeliveryMethodProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub DiscountRulesProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""
            sAdminLayout = "DiscountRules"
            Dim cSql As String
            Dim oDS As DataSet
            Try
                cSql = "Select *, a.nStatus as status, a.dPublishDate as publishDate, a.dExpireDate as expireDate From tblCartDiscountRules dr inner join tblaudit a on dr.nAuditid = a.nAuditKey"
                oDS = myWeb.moDbHelper.GetDataSet(cSql, "DiscountRule", "DiscountRules")
                If oDS.Tables.Count = 1 Then
                    oDS.Tables(0).Columns("nDiscountKey").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("nDiscountForeignRef").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("cDiscountName").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("cDiscountCode").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("bDiscountIsPercent").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nDiscountCompoundBehaviour").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nDiscountValue").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nDiscountMinPrice").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nDiscountMinQuantity").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nDiscountCat").ColumnMapping = MappingType.Element
                    oDS.Tables(0).Columns("nAuditId").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("status").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("publishDate").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("expireDate").ColumnMapping = MappingType.Attribute
                    oDS.Tables(0).Columns("cAdditionalXML").ColumnMapping = MappingType.Element

                    If myWeb.moDbHelper.checkTableColumnExists("tblCartShippingPermission", "nPermLevel") Then
                        cSql = "SELECT tblDirectory.*, tblCartDiscountDirRelations.nDiscountDirRelationKey, tblCartDiscountDirRelations.nPermLevel, tblCartDiscountDirRelations.nDiscountId FROM tblCartDiscountDirRelations LEFT OUTER JOIN tblDirectory ON tblCartDiscountDirRelations.nDirId = tblDirectory.nDirKey WHERE (tblCartDiscountDirRelations.nDiscountDirRelationKey IS NOT NULL)"
                    Else
                        cSql = "SELECT tblDirectory.*, tblCartDiscountDirRelations.nDiscountDirRelationKey, tblCartDiscountDirRelations.nDiscountId FROM tblCartDiscountDirRelations LEFT OUTER JOIN tblDirectory ON tblCartDiscountDirRelations.nDirId = tblDirectory.nDirKey WHERE (tblCartDiscountDirRelations.nDiscountDirRelationKey IS NOT NULL)"
                    End If

                    myWeb.moDbHelper.addTableToDataSet(oDS, cSql, "Dir")
                    cSql = "SELECT tblCartProductCategories.*, tblCartDiscountProdCatRelations.nDiscountProdCatRelationKey, tblCartDiscountProdCatRelations.nProductCatId, tblCartDiscountProdCatRelations.nDiscountId FROM tblCartProductCategories RIGHT OUTER JOIN tblCartDiscountProdCatRelations ON tblCartProductCategories.nCatKey = tblCartDiscountProdCatRelations.nProductCatId" ' WHERE (tblCartProductCategories.cCatSchemaName = N'Discount')"
                    myWeb.moDbHelper.addTableToDataSet(oDS, cSql, "ProdCat")
                    If oDS.Tables.Contains("Dir") Then
                        oDS.Relations.Add("RelDiscDir", oDS.Tables("DiscountRule").Columns("nDiscountKey"), oDS.Tables("Dir").Columns("nDiscountId"), False)
                        oDS.Relations("RelDiscDir").Nested = True
                        oDS.Tables("Dir").Columns("nDirKey").ColumnMapping = MappingType.Attribute
                        oDS.Tables("Dir").Columns("cDirName").ColumnMapping = MappingType.Attribute
                        If myWeb.moDbHelper.checkTableColumnExists("tblCartShippingPermission", "nPermLevel") Then
                            oDS.Tables("Dir").Columns("nPermLevel").ColumnMapping = MappingType.Attribute
                        End If
                    End If
                    If oDS.Tables.Contains("ProdCat") Then
                        oDS.Relations.Add("RelDiscProdCat", oDS.Tables("DiscountRule").Columns("nDiscountKey"), oDS.Tables("ProdCat").Columns("nDiscountId"), False)
                        oDS.Relations("RelDiscProdCat").Nested = True
                        oDS.Tables("ProdCat").Columns("nCatKey").ColumnMapping = MappingType.Attribute
                        oDS.Tables("ProdCat").Columns("cCatName").ColumnMapping = MappingType.Attribute
                    End If
                End If
                Dim oElmt As XmlElement = oPageDetail.OwnerDocument.CreateElement("DiscountRules")
                oElmt.InnerXml = Replace(Replace(oDS.GetXml, "&lt;", "<"), "&gt;", ">")
                oPageDetail.AppendChild(oElmt.FirstChild)
            Catch ex As Exception
                returnException(mcModuleName, "DiscountRulesProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub updateLessVariables(ByVal ThemeName As String, ByRef settingsXml As XmlElement)
            Dim cProcessInfo As String = ""
            Dim ThemeLessFile As String = ""
            Try
                If Not settingsXml.SelectSingleNode("theme/add[@key='variablesPath']") Is Nothing Then
                    ThemeLessFile = settingsXml.SelectSingleNode("theme/add[@key='variablesPath']/@value").InnerText
                End If
                If ThemeLessFile <> "" Then
                    Dim oFsH As New Eonic.fsHelper(myWeb.moCtx)

                    ThemeLessFile = fsHelper.checkLeadingSlash(ThemeLessFile)
                    ThemeLessFile = "/ewThemes/" & ThemeName & "/" & ThemeLessFile
                    If oFsH.VirtualFileExists(ThemeLessFile) Then



                        Dim oImp As Eonic.Tools.Security.Impersonate = New Eonic.Tools.Security.Impersonate
                        If oImp.ImpersonateValidUser(myWeb.moConfig("AdminAcct"), myWeb.moConfig("AdminDomain"), myWeb.moConfig("AdminPassword"), , myWeb.moConfig("AdminGroup")) Then

                            Dim content As String

                            'check not read only
                            Dim oFileInfo As IO.FileInfo = New IO.FileInfo(myWeb.goServer.MapPath(ThemeLessFile))
                            oFileInfo.IsReadOnly = False

                            Using reader As New StreamReader(myWeb.goServer.MapPath(ThemeLessFile))
                                content = reader.ReadToEnd()
                                reader.Close()
                            End Using

                            Dim oElmt As XmlElement
                            For Each oElmt In settingsXml.SelectNodes("theme/add[starts-with(@key,'" & ThemeName & ".')]")
                                Dim variableName As String = oElmt.GetAttribute("key").Replace(ThemeName & ".", "")
                                Dim searchText As String = "(?<=@" & variableName & ":).*(?=;)"
                                Dim replaceText As String = oElmt.GetAttribute("value").Trim

                                'handle image files in CSS
                                If LCase(replaceText).EndsWith(".gif") Or LCase(replaceText).EndsWith(".png") Or LCase(replaceText).EndsWith(".jpg") Then
                                    replaceText = " '" & replaceText & "'"
                                Else
                                    replaceText = " " & replaceText
                                End If

                                content = Regex.Replace(content, searchText, replaceText)
                            Next

                            Using writer As New StreamWriter(myWeb.goServer.MapPath(ThemeLessFile))
                                writer.Write(content)
                                writer.Close()
                            End Using

                            oImp.UndoImpersonation()
                        End If
                    End If

                End If

            Catch ex As Exception
                returnException(mcModuleName, "updateLessVariables", ex, "", cProcessInfo, gbDebug)
            End Try

        End Sub


        Private Sub updateStandardXslVariables(ByVal ThemeName As String, ByRef settingsXml As XmlElement)
            Dim cProcessInfo As String = ""
            Dim ThemeXslFile As String = ""
            Try

                Dim oFsH As New Eonic.fsHelper(myWeb.moCtx)


                ThemeXslFile = "/ewThemes/" & ThemeName & "/Standard.xsl"
                If oFsH.VirtualFileExists(ThemeXslFile) Then



                    Dim oImp As Eonic.Tools.Security.Impersonate = New Eonic.Tools.Security.Impersonate
                    If oImp.ImpersonateValidUser(myWeb.moConfig("AdminAcct"), myWeb.moConfig("AdminDomain"), myWeb.moConfig("AdminPassword"), , myWeb.moConfig("AdminGroup")) Then

                        Dim content As String

                        'check not read only
                        Dim oFileInfo As IO.FileInfo = New IO.FileInfo(myWeb.goServer.MapPath(ThemeXslFile))
                        oFileInfo.IsReadOnly = False

                        Using reader As New StreamReader(myWeb.goServer.MapPath(ThemeXslFile))
                            content = reader.ReadToEnd()
                            reader.Close()
                        End Using

                        Dim oElmt As XmlElement
                        For Each oElmt In settingsXml.SelectNodes("theme/add[starts-with(@key,'" & ThemeName & ".')]")
                            Dim variableName As String = oElmt.GetAttribute("key").Replace(ThemeName & ".", "")

                            Dim searchText As String = "(?<=<xsl:variable name=""" & variableName & """>).*(?=</xsl:variable>)"

                            Dim replaceText As String = oElmt.GetAttribute("value").Trim

                            content = Regex.Replace(content, searchText, replaceText)
                        Next

                        Using writer As New StreamWriter(myWeb.goServer.MapPath(ThemeXslFile))
                            writer.Write(content)
                            writer.Close()
                        End Using

                        oImp.UndoImpersonation()
                    End If
                End If

            Catch ex As Exception
                returnException(mcModuleName, "updateStandardXslVariables", ex, "", cProcessInfo, gbDebug)
            End Try

        End Sub


        Public Sub SchedulerProcess(ByRef cewCmd As String, ByRef cLayout As String, ByRef oContentDetail As XmlElement)
            'Dim oScheduler As New Scheduler
            Dim oSchedulerConfig As System.Collections.Specialized.NameValueCollection = WebConfigurationManager.GetWebApplicationSection("eonic/scheduler")
            Dim bHasScheduledItems As Boolean = False
            Dim cConStr As String = ""

            Dim cProcessInfo As String = ""

            Dim cUrl As String = moConfig("BaseUrl")

            ' DBHelper for the Scheduler database
            Dim dbt As New dbHelper(myWeb)

            Try
                If Not oSchedulerConfig Is Nothing Then
                    cProcessInfo = "Connecting to the Scheduler"
                    If Not oSchedulerConfig("SiteURL") = "" Then cUrl = oSchedulerConfig("SiteURL")
                    bHasScheduledItems = True
                    cConStr = "Data Source=" & oSchedulerConfig("DatabaseServer") & "; "
                    cConStr &= "Initial Catalog=" & oSchedulerConfig("DatabaseName") & "; "

                    If oSchedulerConfig("DatabaseAuth") <> "" Then
                        cConStr &= oSchedulerConfig("DatabaseAuth")
                    Else
                        cConStr &= "user id=" & oSchedulerConfig("DatabaseUsername") & ";password=" & oSchedulerConfig("DatabasePassword") & ";"
                    End If

                    dbt.ResetConnection(cConStr)
                End If
Process:


                cProcessInfo = "Process: " & cewCmd
                Select Case cewCmd
                    Case "ScheduledItems"
                        cLayout = "ScheduledItems"
                        If dbt.ConnectionValid = False Then
                            Dim oElmt As XmlElement = oContentDetail.OwnerDocument.CreateElement("Content")
                            oElmt.SetAttribute("type", "ActionList")
                            Dim oSubElmt As XmlElement = oContentDetail.OwnerDocument.CreateElement("Item")
                            oSubElmt.InnerText = "Connection invalid please speak to server administrator - " & dbt.DatabaseConnectionString
                            oElmt.AppendChild(oSubElmt)
                            oContentDetail.AppendChild(oElmt)
                        Else

                            'oScheduler.ListActions(oContentDetail)
                            If oSchedulerConfig Is Nothing Then Exit Sub
                            Dim oList() As String = Split(oSchedulerConfig("AvailableActions"), ",")
                            Dim oDS As DataSet
                            'create new dbtools so we can use the scheduler DB
                            Dim i As Integer
                            Dim oElmt As XmlElement = oContentDetail.OwnerDocument.CreateElement("Content")
                            oElmt.SetAttribute("type", "ActionList")
                            For i = 0 To UBound(oList)
                                Dim oSubElmt As XmlElement = oContentDetail.OwnerDocument.CreateElement("Item")
                                oSubElmt.InnerText = oList(i)
                                oElmt.AppendChild(oSubElmt)
                            Next
                            oContentDetail.AppendChild(oElmt)



                            Dim cSQL As String = "SELECT tblActions.* FROM tblWebsites INNER JOIN tblActions ON tblWebsites.nWebsiteKey = tblActions.nWebsite WHERE (tblWebsites.cWebsiteURL = '" & cUrl & "')"
                            oDS = dbt.GetDataSet(cSQL, "Content")
                            cProcessInfo = "Process: " & cewCmd & " - " & cSQL & " - "

                            If oDS Is Nothing Then
                                'Throw New Exception("Could not get Actions")
                            ElseIf oDS.Tables("Content").Rows.Count > 0 Then
                                oDS.Tables("Content").Columns.Add(New DataColumn("Active", GetType(System.Int16)))
                                Dim oDC As DataColumn
                                For Each oDC In oDS.Tables("Content").Columns
                                    oDC.ColumnMapping = MappingType.Attribute
                                Next
                                oDS.Tables("Content").Columns("cActionXML").ColumnMapping = MappingType.Element
                                Dim oRow As DataRow
                                For Each oRow In oDS.Tables("Content").Rows
                                    Dim bActive As Boolean = True
                                    Dim dDate As Date
                                    If Not IsDBNull(oRow("dPublishDate")) Then
                                        dDate = CDate(oRow("dPublishDate"))
                                        If Not dDate <= Now Then bActive = False
                                    End If

                                    If Not IsDBNull(oRow("dExpireDate")) Then
                                        dDate = CDate(oRow("dExpireDate"))
                                        If Not dDate >= Now Then bActive = False
                                    End If

                                    oRow("Active") = CInt(bActive)
                                Next
                                Dim oXML As New XmlDocument
                                oXML.InnerXml = Replace(Replace(oDS.GetXml, "&gt;", ">"), "&lt;", "<")
                                oContentDetail.InnerXml &= oXML.DocumentElement.InnerXml
                            End If
                        End If

                    Case "AddScheduledItem", "EditScheduledItem"
                        Dim oADX As New AdminXforms(myWeb)

                        Dim nID As Integer = 0
                        Dim cSQL As String = "SELECT nWebsiteKey FROM tblWebsites WHERE (cWebsiteURL = '" & cUrl & "')"
                        cProcessInfo = "Process: " & cewCmd & " - " & cSQL

                        dbt.ResetConnection(cConStr)
                        nID = dbt.ExeProcessSqlScalar(cSQL)
                        If Not nID > 0 Then
                            cSQL = "INSERT INTO tblWebsites (cWebsiteURL) VALUES ('" & cUrl & "')"
                            cProcessInfo = "Process: " & cewCmd & " - " & cSQL
                            nID = dbt.GetIdInsertSql(cSQL)
                        End If
                        oContentDetail.AppendChild(oContentDetail.OwnerDocument.ImportNode(oADX.xFrmSchedulerItem(myWeb.moRequest("type"), nID, cConStr, myWeb.moRequest("id")), True))
                        If oADX.valid Then
                            cewCmd = "ScheduledItems"
                            GoTo Process
                        Else
                            cLayout = "EditScheduledItem"
                        End If
                    Case "ScheduledItemRunNow"
                        'TODO
                        Dim oDS As DataSet
                        Dim oRow As DataRow
                        Dim cSQL As String = "SELECT tblActions.* FROM tblWebsites INNER JOIN tblActions ON tblWebsites.nWebsiteKey = tblActions.nWebsite WHERE (tblWebsites.cWebsiteURL = '" & cUrl & "' and tblActions.nActionKey = " & myWeb.moRequest("id") & ")"
                        oDS = dbt.GetDataSet(cSQL, "Action")
                        For Each oRow In oDS.Tables("Action").Rows
                            Dim oTimeStart As Date = Now
                            Dim oSoapClient As New Eonic.Tools.SoapClient
                            oSoapClient.RemoveReturnSoapEnvelope = True
                            oSoapClient.Url = cUrl & "/" & oRow("cSubPath")
                            oSoapClient.Action = oRow("cType")
                            Dim ActionXml As String = oRow("cActionXml")
                            Dim oXML As New XmlDocument
                            oXML.LoadXml(ActionXml)
                            If Not oXML.DocumentElement.GetAttribute("xmlns") = "" Then
                                oSoapClient.Namespace = oXML.DocumentElement.GetAttribute("xmlns")
                            Else
                                oSoapClient.Namespace = oXML.DocumentElement.GetAttribute("exemelnamespace")
                            End If
                            Dim cResponse As String = oSoapClient.SendRequest(ActionXml)

                            oContentDetail.InnerXml = cResponse
                        Next

                    Case "DeactivateScheduledItem"
                        Dim cSQL As String
                        Dim dPublishDate As String
                        Dim dExpireDate As String

                        Dim cTime As String = IIf(Now.Hour < 10, "0" & Now.Hour, Now.Hour) & ":" & IIf(Now.Minute < 10, "0" & Now.Minute, Now.Minute) & ":" & IIf(Now.Second < 10, "0" & Now.Second, Now.Second)

                        dPublishDate = "Null"
                        'dExpireDate = sqlDateTime(Now, cTime)
                        dExpireDate = Eonic.Tools.Database.SqlDate(Now, True)

                        cSQL = "UPDATE tblActions SET dPublishDate =" & dPublishDate & ", dExpireDate =" & dExpireDate & " WHERE nActionKey = " & myWeb.moRequest("id")
                        cProcessInfo = "Process: " & cewCmd & " - " & cSQL

                        dbt.ExeProcessSql(cSQL)

                        cewCmd = "ScheduledItems"
                        GoTo Process

                    Case "ActivateScheduledItem"
                        Dim cSQL As String
                        Dim dPublishDate As String
                        Dim dExpireDate As String

                        'Dim cTime As String = IIf(Now.Hour < 10, "0" & Now.Hour, Now.Hour) & ":" & IIf(Now.Minute < 10, "0" & Now.Minute, Now.Minute) & ":" & IIf(Now.Second < 10, "0" & Now.Second, Now.Second)

                        'dPublishDate = sqlDateTime(Now, cTime)
                        dPublishDate = Eonic.Tools.Database.SqlDate(Now, True)
                        dExpireDate = "Null"

                        cSQL = "UPDATE tblActions SET dPublishDate =" & dPublishDate & ", dExpireDate =" & dExpireDate & " WHERE nActionKey = " & myWeb.moRequest("id")
                        cProcessInfo = "Process: " & cewCmd & " - " & cSQL

                        dbt.ExeProcessSql(cSQL)

                        cewCmd = "ScheduledItems"
                        GoTo Process
                End Select
            Catch ex As Exception
                returnException(mcModuleName, "SchedulerProcess", ex, "", cProcessInfo, gbDebug)
            End Try
        End Sub

        Public Sub SubscriptionProcess(ByRef cCmd As String, ByRef sAdminLayout As String, ByRef oPageDetail As XmlElement)
            Dim oSub As New Eonic.Web.Cart.Subscriptions(myWeb)
            Dim oADX As New AdminXforms(myWeb)
SP:
            Select Case cCmd
                Case "CancelSubscription"
                    oSub.CancelSubscription(myWeb.moRequest("subId"))
                    cCmd = "ListSubscribers"
                    GoTo SP
                Case "Subscriptions"
                    oSub.ListSubscriptions(oPageDetail)
                Case "ListSubscribers"
                    oSub.ListSubscribers(oPageDetail)
                Case "AddSubscriptionGroup"
                    oPageDetail.AppendChild(oPageDetail.OwnerDocument.ImportNode(oADX.xFrmProductGroup(0, "Subscription"), True))
                    If oADX.valid Then
                        cCmd = "Subscriptions"
                        GoTo SP
                    Else
                        sAdminLayout = "AdminXForm"
                    End If
                Case "EditSubscriptionGroup"
                    oPageDetail.AppendChild(oPageDetail.OwnerDocument.ImportNode(oADX.xFrmProductGroup(myWeb.moRequest("grp"), "Subscription"), True))
                    If oADX.valid Then
                        cCmd = "Subscriptions"
                        GoTo SP
                    Else
                        sAdminLayout = "AdminXForm"
                    End If
                Case "AddSubscription"
                    oPageDetail.AppendChild(oPageDetail.OwnerDocument.ImportNode(oADX.xFrmEditSubscription(0, 0, myWeb.moRequest("grp")), True))
                    If oADX.valid Then
                        cCmd = "Subscriptions"
                        GoTo SP
                    Else
                        sAdminLayout = "AdminXForm"
                    End If
                Case "EditSubscription"
                    oPageDetail.AppendChild(oPageDetail.OwnerDocument.ImportNode(oADX.xFrmEditSubscription(myWeb.moRequest("id"), 0, IIf(IsNumeric(myWeb.moRequest("grp")), myWeb.moRequest("grp"), 0)), True))
                    If oADX.valid Then
                        cCmd = "Subscriptions"
                        GoTo SP
                    Else
                        sAdminLayout = "AdminXForm"
                    End If
                Case "EditUserSubscription"
                    'oPageDetail.AppendChild(oPageDetail.OwnerDocument.ImportNode(oADX.), True))
                    'If oADX.valid Then
                    '    cCmd = "Subscriptions"
                    '    GoTo SP
                    'Else
                    '    sAdminLayout = "AdminXForm"
                    'End If
                Case "MoveSubscription"

                    sAdminLayout = "Subscriptions"
                    If IsNumeric(myWeb.moRequest("grp")) Then
                        oSub.SubscriptionToGroup(myWeb.moRequest("id"), myWeb.moRequest("grp"))
                        cCmd = "Subscriptions"
                        GoTo SP
                    Else
                        oSub.ListSubscriptions(oPageDetail)
                        cCmd = "MoveSubscription"
                    End If

                Case "LocateSubscription"
                    If myWeb.moRequest("submit") <> "" Then
                        'updateLocations
                        myWeb.moDbHelper.updateLocations(myWeb.moRequest("id"), myWeb.moRequest("location"))
                        sAdminLayout = "Subscriptions"
                        mcEwCmd = "Subscriptions"

                        oPageDetail.RemoveAll()
                        myWeb.mnArtId = 0
                        GoTo SP
                    Else
                        oPageDetail.AppendChild(myWeb.moDbHelper.getLocationsByContentId(myWeb.moRequest("id")))
                        sAdminLayout = "LocateContent"
                        mcEwCmd = "LocateSubscription"

                    End If
                Case "UpSubscription"

                Case "DownSubscription"

            End Select


        End Sub

        'VersionControlProcess
        Public Sub VersionControlProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Try
                sAdminLayout = "VersionControlProcess"

                ' Go get all the content that is in a state of pending

                ' Go get all the versions that are pending
                Dim oContElmt As XmlElement = myWeb.moDbHelper.getPendingContent()

                If Not (oContElmt Is Nothing) Then oPageDetail.AppendChild(oContElmt)


            Catch ex As Exception
                returnException(mcModuleName, "VersionControlProcess", ex, "", "", gbDebug)
            End Try
        End Sub


        Public Sub MemberActivityProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Try
                sAdminLayout = "MemberActivityReport"
                Dim oDS As New DataSet
                Dim cReportName As String = ""
                If Not myWeb.moRequest("SessionID") = "" Then
                    myWeb.moDbHelper.ConnectTimeout = 180
                    oDS = myWeb.moDbHelper.GetDataSet("EXEC spGetUserSessionActivityDetail '" & myWeb.moRequest("SessionId") & "'", "Item", "Report")
                    myWeb.moDbHelper.ConnectTimeout = 15
                    cReportName = "User Session Report"
                ElseIf Not myWeb.moRequest("UserId") = "" Then
                    myWeb.moDbHelper.ConnectTimeout = 180
                    oDS = myWeb.moDbHelper.GetDataSet("EXEC spGetUserSessionActivity " & myWeb.moRequest("UserId"), "Item", "Report")
                    myWeb.moDbHelper.ConnectTimeout = 15
                    cReportName = "User Activity Report"
                Else
                    moAdXfm.xFrmMemberVisits()
                    If moAdXfm.valid Then
                        Dim dFrom As Date
                        Dim dTo As Date
                        If IsDate(moAdXfm.Instance.FirstChild.SelectSingleNode("dTo").InnerText) Then dFrom = CDate(moAdXfm.Instance.FirstChild.SelectSingleNode("dFrom").InnerText)
                        If IsDate(moAdXfm.Instance.FirstChild.SelectSingleNode("dTo").InnerText) Then dTo = CDate(moAdXfm.Instance.FirstChild.SelectSingleNode("dTo").InnerText)
                        Dim cFrom As String = Eonic.Tools.Database.SqlDate(dFrom, False)
                        Dim cTo As String = Eonic.Tools.Database.SqlDate(dTo, False)
                        Dim cGroups As String = "'" & moAdXfm.Instance.FirstChild.SelectSingleNode("cGroups").InnerText & "'"
                        Dim sSql As String = "EXEC spGetSessionActivity " & cFrom & ", " & cTo & ", " & cGroups
                        myWeb.moDbHelper.ConnectTimeout = 180
                        oDS = myWeb.moDbHelper.GetDataSet(sSql, "Item", "Report")
                        myWeb.moDbHelper.ConnectTimeout = 15
                        cReportName = "Member Activity Report"
                    End If
                    oPageDetail.AppendChild(moAdXfm.moXformElmt)
                End If

                If oDS.Tables.Count > 0 Then
                    Dim oContElmt As XmlElement = moPageXML.CreateElement("Content")
                    oContElmt.SetAttribute("name", cReportName)
                    oContElmt.SetAttribute("type", "Report")
                    oContElmt.InnerXml = oDS.GetXml
                    oPageDetail.AppendChild(oContElmt)
                End If


            Catch ex As Exception
                returnException(mcModuleName, "MemberActivityProcess", ex, "", "", gbDebug)
            End Try
        End Sub

        ''' <summary>
        ''' The Member code management process.
        ''' </summary>
        ''' <param name="oPageDetail">The page ContentDetail node</param>
        ''' <param name="sAdminLayout">Referential variable for the admin layout</param>
        ''' <remarks></remarks>
        Public Sub MemberCodesProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)

            Dim cProcessInfo As String = ""
            PerfMon.Log("Admin", "MemberCodesProcess")

            Try

                Dim cSQL As String = ""
                Dim oDS As DataSet
                Dim oXml As XmlDataDocument
                Dim oElmt As XmlElement
                Dim sContent As String = ""
                Dim nId As Integer

                Dim oCont As XmlElement = moPageXML.CreateElement("Content")

                Dim bListCodesets As Boolean = True
                Dim cSubCmd As String = ""


                ' We are either dealing with a form or not - if not just return the codesets
                If IsNumeric(myWeb.moRequest("id")) Or myWeb.moRequest("subCmd") <> "" Then

                    cProcessInfo = "Process form"
                    bListCodesets = False

                    ' Get the appropriate form
                    cSubCmd = myWeb.moRequest("subCmd")
                    If cSubCmd = "" Then cSubCmd = "EditCodeSet"

                    ' Set variables
                    nId = myWeb.moRequest("id")

                    Select Case cSubCmd

                        Case "EditCodeSet", "AddCodeSet"
                            ' This is add or edit
                            moAdXfm.xFrmMemberCodeset(nId)
                            If moAdXfm.isSubmitted AndAlso moAdXfm.valid Then bListCodesets = True

                        Case "ManageCodes"
                            moAdXfm.xFrmMemberCodeGenerator(nId)

                            ' Get the list of sub codes.
                            If nId = 0 Then nId = -1
                            cSQL = "SELECT nCodeKey As id, cCode As Code, dUseDate As Date_Used, nDirKey As User_Id, cDirName As Username, cDirXml As UserXml FROM tblCodes LEFT OUTER JOIN tblDirectory ON nUseId = nDirKey WHERE nCodeParentId = " & nId

                            oDS = myWeb.moDbHelper.GetDataSet(cSQL, "Code", "tblCodes")
                            oDS.Tables("Code").Columns("id").ColumnMapping = MappingType.Attribute
                            oDS.Tables("Code").Columns("User_Id").ColumnMapping = MappingType.Attribute

                            myWeb.moDbHelper.ReturnNullsEmpty(oDS)
                            oDS.EnforceConstraints = False
                            oXml = New XmlDataDocument(oDS)

                            'Convert any text to xml
                            For Each oElmt In oXml.SelectNodes("descendant-or-self::UserXml")
                                sContent = oElmt.InnerText
                                If sContent <> "" Then
                                    oElmt.InnerXml = sContent
                                End If
                            Next

                            oCont.InnerXml = oXml.InnerXml
                            oCont.SetAttribute("type", "SubCodeList")
                            oCont.SetAttribute("name", "Directory Codes")
                            oPageDetail.AppendChild(oCont)

                        Case "ManageCodeGroups"
                            moAdXfm.xFrmMemberCodeset(nId, "CodesGroups")
                            If moAdXfm.isSubmitted AndAlso moAdXfm.valid Then bListCodesets = True
                    End Select

                    If Not bListCodesets Then oPageDetail.AppendChild(moAdXfm.moXformElmt)

                End If

                If bListCodesets Then

                    cProcessInfo = "Get codeset list"

                    'Show CodeSet List
                    cSQL = "exec spGetCodes " & Eonic.Web.dbHelper.CodeType.Membership
                    oDS = myWeb.moDbHelper.GetDataSet(cSQL, "Code", "tblCodes")

                    ' Enumerate the group memberships for each
                    Dim cGroups As String = ""
                    For Each oRow As DataRow In oDS.Tables("Code").Rows
                        If Not cGroups = "" Then cGroups &= ","
                        cGroups &= oRow("nCodeKey")
                    Next
                    myWeb.moDbHelper.addTableToDataSet(oDS, "exec spGetCodeDirectoryGroups '" & cGroups & "'", "Groups")
                    For Each oDT As DataTable In oDS.Tables
                        For Each oDC As DataColumn In oDT.Columns
                            oDC.ColumnMapping = MappingType.Attribute
                        Next
                    Next

                    ' Add the Groups to the Dataset
                    oDS.Tables("Groups").Columns("nCodeKey").ColumnMapping = MappingType.Hidden
                    oDS.Relations.Add("Rel01", oDS.Tables("Code").Columns("nCodeKey"), oDS.Tables("Groups").Columns("nCodeKey"), False)
                    oDS.Relations("Rel01").Nested = True

                    ' Convert the results to XML
                    oCont.InnerXml = oDS.GetXml
                    oCont.SetAttribute("type", "CodeSet")
                    oCont.SetAttribute("name", "Directory Codes")
                    oPageDetail.AppendChild(oCont)

                End If

                sAdminLayout = "DirectoryCodes"

            Catch ex As Exception
                returnException(mcModuleName, "MemberCodesProcess", ex, "", cProcessInfo, gbDebug)
            End Try
        End Sub

        Private Sub ReportsProcess(ByRef oPageDetail As XmlElement, ByRef sAdminLayout As String)
            Dim sProcessInfo As String = ""

            Try
                'Case "cpdReportsPage"

                If myWeb.moRequest("ewCmd2") <> "" Then
                    oPageDetail.AppendChild(moAdXfm.xFrmGetReport(myWeb.moRequest("ewCmd2")))
                    If moAdXfm.valid Then
                        myWeb.moDbHelper.GetReport(oPageDetail, moAdXfm.Instance.FirstChild)
                    End If
                End If

                If oPageDetail.InnerXml = "" Then
                    myWeb.moDbHelper.ListReports(oPageDetail)
                End If

            Catch ex As Exception
                returnException(mcModuleName, "DeliveryMethodProcess", ex, "", sProcessInfo, gbDebug)
            End Try
        End Sub

    End Class

    
End Class
