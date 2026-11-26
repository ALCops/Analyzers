using System.Collections.Immutable;
using Microsoft.Dynamics.Nav.CodeAnalysis;
using Microsoft.Dynamics.Nav.CodeAnalysis.Diagnostics;
using Microsoft.Dynamics.Nav.CodeAnalysis.Syntax;
using RoslynTestKit;

namespace ALCops.LinterCop.Test
{
    public class TranslatableTextShouldBeTranslated : NavCodeAnalysisBase
    {
        private string _testCasePath;

        private static readonly byte[] EmptyXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                  </group>
                </body>
              </file>
            </xliff>
            """);

        private static readonly byte[] TranslatedReportLabelXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Report 2858589782 - ReportLabel 973805576" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Report Label Text</source>
                      <target>Berichtsbezeichnungstext</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportLabel MyReportLabel</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Object-level ToolTip trans-units for page MyPage and report MyReport: the object segment
        // followed by the hashed canonical property name ("ToolTip" hashes to 1295455071).
        private static readonly byte[] TranslatedObjectToolTipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Shows my page.</source>
                      <target>Viser min side.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property ToolTip</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Runs my report.</source>
                      <target>Kører min rapport.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - Property ToolTip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Object-level translatable properties other than Caption and ToolTip, copied from the
        // Translations/*.g.xlf the AL compiler generates for the matching fixtures (app.json with
        // "features": ["TranslationFile"]). A page extension or profile extension in the same module
        // as its target folds into the target, so its trans-units carry the target's ID ("Page MyPage",
        // "Profile MyProfile"); a page customization keeps its own ID ("PageCustomization MyPageCust").
        private static readonly byte[] TranslatedObjectPropertiesXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My page explains itself.</source>
                      <target>DA My page explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my page</source>
                      <target>DA About my page</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Property 3863440606" size-unit="char" translate="yes" xml:space="preserve">
                      <source>my search terms</source>
                      <target>DA my search terms</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property AdditionalSearchTerms</note>
                    </trans-unit>
                    <trans-unit id="Page 904567593 - Property 3446740159" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Entity</source>
                      <target>DA My Entity</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyApiPage - Property EntityCaption</note>
                    </trans-unit>
                    <trans-unit id="Page 904567593 - Property 631549417" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Entities</source>
                      <target>DA My Entities</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyApiPage - Property EntitySetCaption</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Property 1968111052" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Fill in my page.</source>
                      <target>DA Fill in my page.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property InstructionalText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Property 2019332006" size-unit="char" translate="yes" xml:space="preserve">
                      <source>New,Process,Report</source>
                      <target>DA New,Process,Report</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Property PromotedActionCategories</note>
                    </trans-unit>
                    <trans-unit id="PageCustomization 4276993442 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My customized page explains itself.</source>
                      <target>DA My customized page explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageCustomization MyPageCust - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="PageCustomization 4276993442 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my customized page</source>
                      <target>DA About my customized page</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageCustomization MyPageCust - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="PageCustomization 4276993442 - Property 2019332006" size-unit="char" translate="yes" xml:space="preserve">
                      <source>New,Process,Report</source>
                      <target>DA New,Process,Report</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageCustomization MyPageCust - Property PromotedActionCategories</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - RequestPage 2516438534 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My request page explains itself.</source>
                      <target>DA My request page explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - RequestPage RequestOptionsPage - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - RequestPage 2516438534 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my request page</source>
                      <target>DA About my request page</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - RequestPage RequestOptionsPage - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - RequestPage 2516438534 - Property 1968111052" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Fill in my request page.</source>
                      <target>DA Fill in my request page.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - RequestPage RequestOptionsPage - Property InstructionalText</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - Property 3863440606" size-unit="char" translate="yes" xml:space="preserve">
                      <source>my report terms</source>
                      <target>DA my report terms</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - Property AdditionalSearchTerms</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My query explains itself.</source>
                      <target>DA My query explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my query</source>
                      <target>DA About my query</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - Property 3446740159" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Query Entity</source>
                      <target>DA My Query Entity</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - Property EntityCaption</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - Property 631549417" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Query Entities</source>
                      <target>DA My Query Entities</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - Property EntitySetCaption</note>
                    </trans-unit>
                    <trans-unit id="Profile 1886450737 - Property 4111922599" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Describes my profile.</source>
                      <target>DA Describes my profile.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Profile MyProfile - Property ProfileDescription</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Control and action translatable properties (InstructionalText, AboutTitle, AboutText), copied
        // from the Translations/*.g.xlf the AL compiler generates for the matching fixtures (app.json with
        // "features": ["TranslationFile"]). A control or action a page extension adds in the same module
        // as its target folds into the target's ID ("Page MyPage - Control MyExtField"), while the
        // generator note keeps the extension's own name.
        private static readonly byte[] TranslatedControlAndActionPropertiesXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Action 1135900470 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My action explains itself.</source>
                      <target>DA My action explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyAction - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 1135900470 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my action</source>
                      <target>DA About my action</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyAction - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 3066929535 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My action group explains itself.</source>
                      <target>DA My action group explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyActionGroup - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 3066929535 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my action group</source>
                      <target>DA About my action group</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyActionGroup - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 3100184595 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My custom action explains itself.</source>
                      <target>DA My custom action explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyCustomAction - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 3100184595 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my custom action</source>
                      <target>DA About my custom action</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyCustomAction - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 2578960685 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Page 2931038265">
                      <source>My extension action explains itself.</source>
                      <target>DA My extension action explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageExtension MyPageExt - Action MyExtAction - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 979848377 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Page 2931038265">
                      <source>About my extension field</source>
                      <target>DA About my extension field</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageExtension MyPageExt - Control MyExtField - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My field explains itself.</source>
                      <target>DA My field explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my field</source>
                      <target>DA About my field</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1968111052" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Fill in my field.</source>
                      <target>DA Fill in my field.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property InstructionalText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 776543033 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My file upload action explains itself.</source>
                      <target>DA My file upload action explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyFileUploadAction - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Action 776543033 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my file upload action</source>
                      <target>DA About my file upload action</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Action MyFileUploadAction - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 894378861 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My group explains itself.</source>
                      <target>DA My group explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyGroup - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 894378861 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my group</source>
                      <target>DA About my group</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyGroup - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 894378861 - Property 1968111052" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Fill in my group.</source>
                      <target>DA Fill in my group.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyGroup - Property InstructionalText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 2443193653 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My part explains itself.</source>
                      <target>DA My part explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyPart - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 2443193653 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my part</source>
                      <target>DA About my part</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyPart - Property AboutTitle</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - Action 1135900470 - Property 247559172" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My request page action explains itself.</source>
                      <target>DA My request page action explains itself.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - Action MyAction - Property AboutText</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - Control 3834966961 - Property 1064389655" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my option</source>
                      <target>DA About my option</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - Control MyOption - Property AboutTitle</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Trans-units for the properties of field groups, page views, report columns, dataitems and
        // layouts, query columns and filters, xmlport table elements, and a table field OptionCaption,
        // copied from the Translations/*.g.xlf the AL compiler generates for the matching fixtures (app.json
        // with "features": ["TranslationFile"]). A nested member hashes under its object only ("Report
        // MyReport - ReportColumn MyColumn", no dataitem segment), and a view, dataitem, column or layout a
        // same-module extension adds folds into the target's ID while the generator note keeps the
        // extension's own name.
        private static readonly byte[] TranslatedChildSymbolPropertiesXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Table 2328808854 - Field 3834966961 - Property 62802879" size-unit="char" translate="yes" xml:space="preserve">
                      <source>First,Second</source>
                      <target>DA First,Second</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Table MyTable - Field MyOption - Property OptionCaption</note>
                    </trans-unit>
                    <trans-unit id="Table 2328808854 - FieldGroup 2253035989 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Drop Down</source>
                      <target>DA My Drop Down</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Table MyTable - FieldGroup DropDown - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - View 415829955 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My View</source>
                      <target>DA My View</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - View MyView - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - View 2298293404 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Page 2931038265">
                      <source>My Extension View</source>
                      <target>DA My Extension View</target>
                      <note from="Xliff Generator" annotates="general" priority="3">PageExtension MyPageExt - View MyExtView - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportColumn 743061902 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Column</source>
                      <target>DA My Column</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportColumn MyColumn - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportColumn 743061902 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Specifies my column.</source>
                      <target>DA Specifies my column.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportColumn MyColumn - Property ToolTip</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportColumn 1643583475 - Property 62802879" size-unit="char" translate="yes" xml:space="preserve">
                      <source>First,Second</source>
                      <target>DA First,Second</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportColumn MyOptionColumn - Property OptionCaption</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportDataItem 2275679299 - Property 1806354803" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Data Item Filters</source>
                      <target>DA My Data Item Filters</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportDataItem MyDataItem - Property RequestFilterHeading</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportLayout 2700067586 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Layout</source>
                      <target>DA My Layout</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportLayout MyLayout - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportLayout 2700067586 - Property 2166945456" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Summarizes my layout.</source>
                      <target>DA Summarizes my layout.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Report MyReport - ReportLayout MyLayout - Property Summary</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportColumn 3647812821 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Report 2858589782">
                      <source>My Extension Column</source>
                      <target>DA My Extension Column</target>
                      <note from="Xliff Generator" annotates="general" priority="3">ReportExtension MyReportExt - ReportColumn MyExtColumn - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportDataItem 4453980 - Property 1806354803" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Report 2858589782">
                      <source>My Extension Data Item Filters</source>
                      <target>DA My Extension Data Item Filters</target>
                      <note from="Xliff Generator" annotates="general" priority="3">ReportExtension MyReportExt - ReportDataItem MyExtDataItem - Property RequestFilterHeading</note>
                    </trans-unit>
                    <trans-unit id="Report 2858589782 - ReportLayout 2429713769 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve" al-object-target="Report 2858589782">
                      <source>My Extension Layout</source>
                      <target>DA My Extension Layout</target>
                      <note from="Xliff Generator" annotates="general" priority="3">ReportExtension MyReportExt - ReportLayout MyExtLayout - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - QueryColumn 743061902 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Column</source>
                      <target>DA My Column</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - QueryColumn MyColumn - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - QueryColumn 743061902 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>Specifies my column.</source>
                      <target>DA Specifies my column.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - QueryColumn MyColumn - Property ToolTip</note>
                    </trans-unit>
                    <trans-unit id="Query 3041816274 - QueryFilter 3760788832 - Property 2879900210" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Filter</source>
                      <target>DA My Filter</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Query MyQuery - QueryFilter MyFilter - Property Caption</note>
                    </trans-unit>
                    <trans-unit id="XmlPort 157304472 - XmlPortNode 3275933160 - Property 1806354803" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Table Element Filters</source>
                      <target>DA My Table Element Filters</target>
                      <note from="Xliff Generator" annotates="general" priority="3">XmlPort MyXmlPort - XmlPortNode MyTableElement - Property RequestFilterHeading</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Compiler-generated trans-unit for a report column Caption in report MyReport in namespace
        // "MyCompany.App" when the TranslationsWithNamespaces feature is enabled.
        private static readonly byte[] NamespaceTranslatedReportColumnCaptionXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Namespace MyCompany.App - Report MyReport - ReportColumn MyColumn - Property Caption" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Column</source>
                      <target>DA My Column</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Namespace MyCompany.App - Report MyReport - ReportColumn MyColumn - Property Caption</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Compiler-generated trans-unit for a group control AboutTitle on page MyPage in namespace
        // "MyCompany.App" when the TranslationsWithNamespaces feature is enabled.
        private static readonly byte[] NamespaceTranslatedPageControlAboutTitleXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Namespace MyCompany.App - Page MyPage - Control MyGroup - Property AboutTitle" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my group</source>
                      <target>DA About my group</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Namespace MyCompany.App - Page MyPage - Control MyGroup - Property AboutTitle</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Compiler-generated trans-unit for page AboutTitle in namespace "MyCompany.App" when the
        // TranslationsWithNamespaces feature is enabled.
        private static readonly byte[] NamespaceTranslatedPageObjectAboutTitleXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Namespace MyCompany.App - Page MyPage - Property AboutTitle" size-unit="char" translate="yes" xml:space="preserve">
                      <source>About my page</source>
                      <target>DA About my page</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Namespace MyCompany.App - Page MyPage - Property AboutTitle</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Trans-unit id for a table Caption in namespace "MyCompany.App" when the compiler feature
        // TranslationsWithNamespaces is enabled: namespace-prefixed, unhashed segments joined by " - ".
        private static readonly byte[] NamespaceTranslatedTableCaptionXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="da-DK" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Namespace MyCompany.App - Table MyTable - Property Caption" size-unit="char" translate="yes" xml:space="preserve">
                      <source>My Table</source>
                      <target>Min tabel</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Table MyTable - Property Caption</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // "Page 2931038265 - Control 1296262074 - Property 1295455071"
        // "Page 2931038265 - Control 2674903734 - Property 1295455071"
        private static readonly byte[] TranslatedPageControlToolTipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property ToolTip</note>
                    </trans-unit>
                    <trans-unit id="Page 2931038265 - Control 2674903734 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is also a tooltip.</source>
                      <target>Dies ist ebenfalls ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control SecondField - Property ToolTip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Legacy runtime (<= Spring2020CU1 / 5.1) hashes the source-cased property name, not the
        // canonical PropertyKind name. For source "Tooltip" the compiler emits hash 2001309823
        // (FNV-1a of UTF-16 "Tooltip") instead of 1295455071 (FNV-1a of "ToolTip"). This XLIFF is
        // what the compiler generates for LegacyRuntimePageControlToolTip.al on runtime <= 5.1;
        // used to verify the analyzer does NOT report a false positive when the runtime-version
        // gate correctly disables the canonical override. If the analyzer applied the canonical
        // override unconditionally, it would hash to 1295455071 and miss this trans-unit, raising
        // a false-positive diagnostic on the fully-translated property.
        private static readonly byte[] LegacySourceCasedTooltipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 2001309823" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property Tooltip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Same fixture as LegacySourceCasedTooltipXliffContent but using the canonical property
        // name hash (1295455071 for "ToolTip") instead of the source-cased hash. Used together
        // with the source-cased AL fixture on a legacy-runtime app.json to prove the analyzer
        // correctly reports a missing translation when the runtime is <= 5.1 (fix scenario:
        // source-cased hash 2001309823 not in XLIFF -> diagnostic). Under the buggy unconditional
        // canonical override, the analyzer would hash to 1295455071 and match this trans-unit,
        // silently missing the diagnostic (false negative).
        private static readonly byte[] LegacyCanonicalTooltipXliffContent = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <xliff version="1.2" xmlns="urn:oasis:names:tc:xliff:document:1.2">
              <file datatype="xml" source-language="en-US" target-language="de-DE" original="TestApp">
                <body>
                  <group id="body">
                    <trans-unit id="Page 2931038265 - Control 1296262074 - Property 1295455071" size-unit="char" translate="yes" xml:space="preserve">
                      <source>This is a tooltip.</source>
                      <target>Dies ist ein ToolTip.</target>
                      <note from="Xliff Generator" annotates="general" priority="3">Page MyPage - Control MyField - Property ToolTip</note>
                    </trans-unit>
                  </group>
                </body>
              </file>
            </xliff>
            """);

        // Minimal app.json declaring runtime 5.1 (Spring2020CU1). Used to force the analyzer down
        // the pre-canonical-override compiler path in LC0091's runtime-version gate.
        private static readonly byte[] AppJsonRuntime51Content = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "id": "00000000-0000-0000-0000-000000000042",
                "name": "TestApp",
                "publisher": "TestPublisher",
                "version": "1.0.0.0",
                "runtime": "5.1",
                "idRanges": [ { "from": 50000, "to": 99999 } ],
                "features": [ "TranslationFile" ]
            }
            """);

        private static readonly byte[] SettingsWithDaDK = System.Text.Encoding.UTF8.GetBytes(
            """{"LanguagesToTranslate": ["da-DK"]}""");

        private static readonly byte[] SettingsWithDaDKAndDeDE = System.Text.Encoding.UTF8.GetBytes(
            """{"LanguagesToTranslate": ["da-DK", "de-DE"]}""");

        [SetUp]
        public void Setup()
        {
            _testCasePath = Path.Combine(
                Directory.GetParent(
                    Environment.CurrentDirectory)!.Parent!.Parent!.FullName,
                    "Rules", nameof(TranslatableTextShouldBeTranslated));
        }

        private static readonly byte[] AnalysisViewDefinitionContent = System.Text.Encoding.UTF8.GetBytes(
            """
            {
                "Id": "00000000-0000-0000-0000-000000000001",
                "Name": "MyAnalysisView",
                "TargetObjectId": 50100,
                "TargetObjectType": "Page"
            }
            """);

        private static AnalyzerTestFixture CreateFixtureWithEmptyXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", EmptyXliffContent },
                { "MyAnalysisView.analysis.json", AnalysisViewDefinitionContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithoutXliff()
        {
            var files = new Dictionary<string, byte[]>();
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithSettings(byte[] settingsContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "alcops.json", settingsContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithXliffAndSettings(byte[] settingsContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", EmptyXliffContent },
                { "alcops.json", settingsContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedReportLabelXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedReportLabelXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedObjectToolTipXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedObjectToolTipXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedObjectPropertiesXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedObjectPropertiesXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedControlAndActionPropertiesXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedControlAndActionPropertiesXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedChildSymbolPropertiesXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", TranslatedChildSymbolPropertiesXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        private static AnalyzerTestFixture CreateFixtureWithTranslatedPageControlToolTipXliff()
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.de-DE.xlf", TranslatedPageControlToolTipXliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        // COMPAT: CompilerFeatures.TranslationsWithNamespaces and CompilationOptions.WithCompilerFeatures are
        // resolved reflectively so this test project still compiles against older SDKs where the enum member is
        // absent. The namespace tests are version-gated (RequireMinimumVersion) so this only runs where present.
        private static Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions CreateNamespaceCompilationOptions()
        {
            var options = new Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions();
            var optionsType = typeof(Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions);
            var featuresType = optionsType.Assembly.GetType("Microsoft.Dynamics.Nav.CodeAnalysis.CompilerFeatures")!;
            var feature = Enum.Parse(featuresType, "TranslationsWithNamespaces");
            var withFeatures = optionsType.GetMethod("WithCompilerFeatures", new[] { featuresType })!;
            return (Microsoft.Dynamics.Nav.CodeAnalysis.CompilationOptions)withFeatures.Invoke(options, new[] { feature })!;
        }

        private static AnalyzerTestFixture CreateFixtureWithNamespaceFeature(byte[] xliffContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "Translations/TestApp.da-DK.xlf", xliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem,
                    CompilationOptions = CreateNamespaceCompilationOptions()
                });
        }

        // Fixture used to exercise LC0091's runtime-version gate. The app.json declares runtime 5.1
        // (Spring2020CU1), which causes the analyzer to hash the source-cased property name instead
        // of the canonical PropertyKind name — matching the compiler's SymbolExtensions.GetTranslationName
        // behavior on legacy runtimes.
        private static AnalyzerTestFixture CreateFixtureWithLegacyRuntime(byte[] xliffContent)
        {
            var files = new Dictionary<string, byte[]>
            {
                { "app.json", AppJsonRuntime51Content },
                { "Translations/TestApp.de-DE.xlf", xliffContent }
            };
            var fileSystem = new MemoryFileSystem(files);

            return RoslynFixtureFactory.Create<Analyzers.TranslatableTextShouldBeTranslated>(
                new AnalyzerTestFixtureConfig
                {
                    FileSystem = fileSystem
                });
        }

        [Test]
        [TestCase("LocalLabel")]
        [TestCase("GlobalLabel")]
        [TestCase("TableFieldCaption")]
        [TestCase("EnumValueCaption")]
        [TestCase("PageControlToolTip")]
        [TestCase("PageAnalysisViewCaption")]
        [TestCase("PageObjectToolTip")]
        [TestCase("ReportLabel")]
        [TestCase("ReportObjectToolTip")]
        [TestCase("PageObjectInstructionalText")]
        [TestCase("PageObjectPromotedActionCategories")]
        [TestCase("PageObjectAdditionalSearchTerms")]
        [TestCase("PageObjectEntityCaption")]
        [TestCase("PageObjectEntitySetCaption")]
        [TestCase("PageObjectAboutTitle")]
        [TestCase("PageObjectAboutText")]
        [TestCase("PageExtensionObjectInstructionalText")]
        [TestCase("PageExtensionObjectPromotedActionCategories")]
        [TestCase("PageExtensionObjectAdditionalSearchTerms")]
        [TestCase("PageExtensionObjectAboutTitle")]
        [TestCase("PageExtensionObjectAboutText")]
        [TestCase("PageCustomizationObjectPromotedActionCategories")]
        [TestCase("PageCustomizationObjectAboutTitle")]
        [TestCase("PageCustomizationObjectAboutText")]
        [TestCase("RequestPageObjectInstructionalText")]
        [TestCase("RequestPageObjectAboutTitle")]
        [TestCase("RequestPageObjectAboutText")]
        [TestCase("ReportObjectAdditionalSearchTerms")]
        [TestCase("QueryObjectEntityCaption")]
        [TestCase("QueryObjectEntitySetCaption")]
        [TestCase("QueryObjectAboutTitle")]
        [TestCase("QueryObjectAboutText")]
        [TestCase("ProfileObjectDescription")]
        [TestCase("ProfileExtensionObjectDescription")]
        [TestCase("PageFieldControlInstructionalText")]
        [TestCase("PageFieldControlAboutTitle")]
        [TestCase("PageFieldControlAboutText")]
        [TestCase("PageGroupControlInstructionalText")]
        [TestCase("PageGroupControlAboutTitle")]
        [TestCase("PageGroupControlAboutText")]
        [TestCase("PagePartControlAboutTitle")]
        [TestCase("PagePartControlAboutText")]
        [TestCase("PageActionAboutTitle")]
        [TestCase("PageActionAboutText")]
        [TestCase("PageActionGroupAboutTitle")]
        [TestCase("PageActionGroupAboutText")]
        [TestCase("PageCustomActionAboutTitle")]
        [TestCase("PageCustomActionAboutText")]
        [TestCase("PageFileUploadActionAboutTitle")]
        [TestCase("PageFileUploadActionAboutText")]
        [TestCase("PageExtensionControlAboutTitle")]
        [TestCase("PageExtensionActionAboutText")]
        [TestCase("RequestPageControlAboutTitle")]
        [TestCase("RequestPageActionAboutText")]
        [TestCase("TableFieldOptionCaption")]
        [TestCase("TableFieldGroupCaption")]
        [TestCase("PageViewCaption")]
        [TestCase("PageExtensionViewCaption")]
        [TestCase("ReportColumnCaption")]
        [TestCase("ReportColumnToolTip")]
        [TestCase("ReportColumnOptionCaption")]
        [TestCase("ReportDataItemRequestFilterHeading")]
        [TestCase("ReportLayoutCaption")]
        [TestCase("ReportLayoutSummary")]
        [TestCase("ReportExtensionColumnCaption")]
        [TestCase("ReportExtensionDataItemRequestFilterHeading")]
        [TestCase("ReportExtensionLayoutCaption")]
        [TestCase("QueryColumnCaption")]
        [TestCase("QueryColumnToolTip")]
        [TestCase("QueryFilterCaption")]
        [TestCase("XmlPortTableElementRequestFilterHeading")]
        [TestCase("TableFieldGroupCaptionInObsoleteRemovedTable")]
        [TestCase("ObsoletePendingTableCaption")]
        [TestCase("ObsoletePendingTableFieldCaption")]
        [TestCase("ObsoletePendingPageControlToolTip")]
        [TestCase("GlobalLabelInObsoleteRemovedTable")]
        [TestCase("LocalLabelInObsoletePendingCodeunit")]
        [TestCase("ReportLabelInObsoletePendingReport")]
        public async Task HasDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["PageAnalysisViewCaption"],
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            SkipTestIfVersionIsTooLow(
                ["PageObjectToolTip"],
                testCase,
                "18.0",
                "ToolTip on a page object is not supported in versions lower than 18.0."
            );

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithEmptyXliff();
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LockedLabel")]
        [TestCase("LockedReportLabel")]
        [TestCase("LockedPageObjectToolTip")]
        [TestCase("LockedReportObjectToolTip")]
        [TestCase("LockedPageObjectInstructionalText")]
        [TestCase("LockedPageObjectPromotedActionCategories")]
        [TestCase("LockedPageObjectAdditionalSearchTerms")]
        [TestCase("LockedPageObjectEntityCaption")]
        [TestCase("LockedPageObjectEntitySetCaption")]
        [TestCase("LockedPageObjectAboutTitle")]
        [TestCase("LockedPageObjectAboutText")]
        [TestCase("LockedPageExtensionObjectInstructionalText")]
        [TestCase("LockedPageExtensionObjectPromotedActionCategories")]
        [TestCase("LockedPageExtensionObjectAdditionalSearchTerms")]
        [TestCase("LockedPageExtensionObjectAboutTitle")]
        [TestCase("LockedPageExtensionObjectAboutText")]
        [TestCase("LockedPageCustomizationObjectPromotedActionCategories")]
        [TestCase("LockedPageCustomizationObjectAboutTitle")]
        [TestCase("LockedPageCustomizationObjectAboutText")]
        [TestCase("LockedRequestPageObjectInstructionalText")]
        [TestCase("LockedRequestPageObjectAboutTitle")]
        [TestCase("LockedRequestPageObjectAboutText")]
        [TestCase("LockedReportObjectAdditionalSearchTerms")]
        [TestCase("LockedQueryObjectEntityCaption")]
        [TestCase("LockedQueryObjectEntitySetCaption")]
        [TestCase("LockedQueryObjectAboutTitle")]
        [TestCase("LockedQueryObjectAboutText")]
        [TestCase("LockedProfileObjectDescription")]
        [TestCase("LockedProfileExtensionObjectDescription")]
        [TestCase("LockedPageFieldControlInstructionalText")]
        [TestCase("LockedPageFieldControlAboutTitle")]
        [TestCase("LockedPageFieldControlAboutText")]
        [TestCase("LockedPageGroupControlInstructionalText")]
        [TestCase("LockedPageGroupControlAboutTitle")]
        [TestCase("LockedPageGroupControlAboutText")]
        [TestCase("LockedPagePartControlAboutTitle")]
        [TestCase("LockedPagePartControlAboutText")]
        [TestCase("LockedPageActionAboutTitle")]
        [TestCase("LockedPageActionAboutText")]
        [TestCase("LockedPageActionGroupAboutTitle")]
        [TestCase("LockedPageActionGroupAboutText")]
        [TestCase("LockedPageCustomActionAboutTitle")]
        [TestCase("LockedPageCustomActionAboutText")]
        [TestCase("LockedPageFileUploadActionAboutTitle")]
        [TestCase("LockedPageFileUploadActionAboutText")]
        [TestCase("LockedPageExtensionControlAboutTitle")]
        [TestCase("LockedPageExtensionActionAboutText")]
        [TestCase("LockedRequestPageControlAboutTitle")]
        [TestCase("LockedRequestPageActionAboutText")]
        [TestCase("LockedTableFieldOptionCaption")]
        [TestCase("LockedTableFieldGroupCaption")]
        [TestCase("LockedPageViewCaption")]
        [TestCase("LockedPageExtensionViewCaption")]
        [TestCase("LockedReportColumnCaption")]
        [TestCase("LockedReportColumnToolTip")]
        [TestCase("LockedReportColumnOptionCaption")]
        [TestCase("LockedReportDataItemRequestFilterHeading")]
        [TestCase("LockedReportLayoutCaption")]
        [TestCase("LockedReportLayoutSummary")]
        [TestCase("LockedReportExtensionColumnCaption")]
        [TestCase("LockedReportExtensionDataItemRequestFilterHeading")]
        [TestCase("LockedReportExtensionLayoutCaption")]
        [TestCase("LockedQueryColumnCaption")]
        [TestCase("LockedQueryColumnToolTip")]
        [TestCase("LockedQueryFilterCaption")]
        [TestCase("LockedXmlPortTableElementRequestFilterHeading")]
        [TestCase("PageAnalysisViewLockedCaption")]
        [TestCase("ObsoleteRemovedTableCaption")]
        [TestCase("ObsoleteRemovedTableFieldCaption")]
        [TestCase("LockedLabelInObsoleteRemovedTable")]
        [TestCase("TableExtensionFieldOnObsoleteRemovedTable")]
        public async Task NoDiagnostic(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["PageAnalysisViewLockedCaption"],
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            SkipTestIfVersionIsTooLow(
                ["LockedPageObjectToolTip"],
                testCase,
                "18.0",
                "ToolTip on a page object is not supported in versions lower than 18.0."
            );

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithEmptyXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("PageControlToolTipWrongCasing")]
        public async Task NoDiagnosticPropertyCasing(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["PageAnalysisViewLockedCaption"],
                testCase,
                "18.0.36",
                "PageAnalysisView requires net10.0 SDK."
            );

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedPageControlToolTipXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedReportLabel")]
        public async Task NoDiagnosticTranslated(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedReportLabelXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedPageObjectToolTip")]
        [TestCase("TranslatedReportObjectToolTip")]
        public async Task NoDiagnosticTranslatedObjectToolTip(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            SkipTestIfVersionIsTooLow(
                ["TranslatedPageObjectToolTip"],
                testCase,
                "18.0",
                "ToolTip on a page object is not supported in versions lower than 18.0."
            );

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedObjectToolTipXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedPageObjectInstructionalText")]
        [TestCase("TranslatedPageObjectPromotedActionCategories")]
        [TestCase("TranslatedPageObjectAdditionalSearchTerms")]
        [TestCase("TranslatedPageObjectEntityCaption")]
        [TestCase("TranslatedPageObjectEntitySetCaption")]
        [TestCase("TranslatedPageObjectAboutTitle")]
        [TestCase("TranslatedPageObjectAboutText")]
        [TestCase("TranslatedPageExtensionObjectInstructionalText")]
        [TestCase("TranslatedPageExtensionObjectPromotedActionCategories")]
        [TestCase("TranslatedPageExtensionObjectAdditionalSearchTerms")]
        [TestCase("TranslatedPageExtensionObjectAboutTitle")]
        [TestCase("TranslatedPageExtensionObjectAboutText")]
        [TestCase("TranslatedPageCustomizationObjectPromotedActionCategories")]
        [TestCase("TranslatedPageCustomizationObjectAboutTitle")]
        [TestCase("TranslatedPageCustomizationObjectAboutText")]
        [TestCase("TranslatedRequestPageObjectInstructionalText")]
        [TestCase("TranslatedRequestPageObjectAboutTitle")]
        [TestCase("TranslatedRequestPageObjectAboutText")]
        [TestCase("TranslatedReportObjectAdditionalSearchTerms")]
        [TestCase("TranslatedQueryObjectEntityCaption")]
        [TestCase("TranslatedQueryObjectEntitySetCaption")]
        [TestCase("TranslatedQueryObjectAboutTitle")]
        [TestCase("TranslatedQueryObjectAboutText")]
        [TestCase("TranslatedProfileObjectDescription")]
        [TestCase("TranslatedProfileExtensionObjectDescription")]
        public async Task NoDiagnosticTranslatedObjectProperties(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedObjectPropertiesXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedPageFieldControlInstructionalText")]
        [TestCase("TranslatedPageFieldControlAboutTitle")]
        [TestCase("TranslatedPageFieldControlAboutText")]
        [TestCase("TranslatedPageGroupControlInstructionalText")]
        [TestCase("TranslatedPageGroupControlAboutTitle")]
        [TestCase("TranslatedPageGroupControlAboutText")]
        [TestCase("TranslatedPagePartControlAboutTitle")]
        [TestCase("TranslatedPagePartControlAboutText")]
        [TestCase("TranslatedPageActionAboutTitle")]
        [TestCase("TranslatedPageActionAboutText")]
        [TestCase("TranslatedPageActionGroupAboutTitle")]
        [TestCase("TranslatedPageActionGroupAboutText")]
        [TestCase("TranslatedPageCustomActionAboutTitle")]
        [TestCase("TranslatedPageCustomActionAboutText")]
        [TestCase("TranslatedPageFileUploadActionAboutTitle")]
        [TestCase("TranslatedPageFileUploadActionAboutText")]
        [TestCase("TranslatedPageExtensionControlAboutTitle")]
        [TestCase("TranslatedPageExtensionActionAboutText")]
        [TestCase("TranslatedRequestPageControlAboutTitle")]
        [TestCase("TranslatedRequestPageActionAboutText")]
        public async Task NoDiagnosticTranslatedControlAndActionProperties(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedControlAndActionPropertiesXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("TranslatedTableFieldOptionCaption")]
        [TestCase("TranslatedTableFieldGroupCaption")]
        [TestCase("TranslatedPageViewCaption")]
        [TestCase("TranslatedPageExtensionViewCaption")]
        [TestCase("TranslatedReportColumnCaption")]
        [TestCase("TranslatedReportColumnToolTip")]
        [TestCase("TranslatedReportColumnOptionCaption")]
        [TestCase("TranslatedReportDataItemRequestFilterHeading")]
        [TestCase("TranslatedReportLayoutCaption")]
        [TestCase("TranslatedReportLayoutSummary")]
        [TestCase("TranslatedReportExtensionColumnCaption")]
        [TestCase("TranslatedReportExtensionDataItemRequestFilterHeading")]
        [TestCase("TranslatedReportExtensionLayoutCaption")]
        [TestCase("TranslatedQueryColumnCaption")]
        [TestCase("TranslatedQueryColumnToolTip")]
        [TestCase("TranslatedQueryFilterCaption")]
        [TestCase("TranslatedXmlPortTableElementRequestFilterHeading")]
        public async Task NoDiagnosticTranslatedChildSymbolProperties(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithTranslatedChildSymbolPropertiesXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        // A view, dataitem or column an extension adds is wrapped in a Change symbol that raises its own
        // symbol event next to the event of the wrapped member. HasDiagnosticAtAllMarkers only checks that a
        // diagnostic exists at each marker, so this test counts them: the single property must yield
        // exactly one diagnostic, at its marker.
        [Test]
        [TestCase("PageExtensionViewCaption")]
        [TestCase("ReportExtensionColumnCaption")]
        [TestCase("ReportExtensionDataItemRequestFilterHeading")]
        public async Task HasSingleDiagnosticForMemberAddedByExtension(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var markup = new CodeMarkup(await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false));

            var compilation = Compilation.Create(
                nameof(HasSingleDiagnosticForMemberAddedByExtension),
                syntaxTrees: new[] { SyntaxTree.ParseObjectText(markup.Code) },
                fileSystem: new MemoryFileSystem(new Dictionary<string, byte[]>
                {
                    { "Translations/TestApp.da-DK.xlf", EmptyXliffContent }
                }));
            var compilationWithAnalyzers = new CompilationWithAnalyzers(
                compilation,
                ImmutableArray.Create<DiagnosticAnalyzer>(new Analyzers.TranslatableTextShouldBeTranslated()),
                options: null!,
                cancellationToken: CancellationToken.None);

            var diagnostics = (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
                .Where(diagnostic => diagnostic.Id == DiagnosticIds.TranslatableTextShouldBeTranslated)
                .ToArray();

            Assert.That(markup.AllLocators, Has.Count.EqualTo(1));
            Assert.That(diagnostics, Has.Length.EqualTo(1));
            Assert.That(markup.AllLocators[0].Match(diagnostics[0].Location), Is.True);
        }

        [Test]
        [TestCase("NoXliffFiles")]
        public async Task NoDiagnosticNoXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithoutXliff();
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LocalLabel")]
        public async Task HasDiagnosticWithLanguagesToTranslateNoXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithSettings(SettingsWithDaDK);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LocalLabel")]
        public async Task HasDiagnosticWithLanguagesToTranslatePartialXliff(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithXliffAndSettings(SettingsWithDaDKAndDeDE);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespaceTableCaption")]
        [TestCase("NamespacePageObjectAboutTitle")]
        [TestCase("NamespacePageControlAboutTitle")]
        [TestCase("NamespaceReportColumnCaption")]
        public async Task HasDiagnosticWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(EmptyXliffContent);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespaceTableCaptionTranslated")]
        public async Task NoDiagnosticWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(NamespaceTranslatedTableCaptionXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespacePageObjectAboutTitleTranslated")]
        public async Task NoDiagnosticTranslatedObjectPropertiesWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(NamespaceTranslatedPageObjectAboutTitleXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespacePageControlAboutTitleTranslated")]
        public async Task NoDiagnosticTranslatedControlAndActionPropertiesWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(NamespaceTranslatedPageControlAboutTitleXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("NamespaceReportColumnCaptionTranslated")]
        public async Task NoDiagnosticTranslatedChildSymbolPropertiesWithNamespaces(string testCase)
        {
            RequireMinimumVersion("18.0.38.52553",
                "Translations with namespaces (CompilerFeatures.TranslationsWithNamespaces) requires the 18.0.38.52553 SDK.");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            var fixture = CreateFixtureWithNamespaceFeature(NamespaceTranslatedReportColumnCaptionXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        // Regression tests for the runtime-version gate around the canonical property-name override.
        // On runtime <= Spring2020CU1 (5.1) the compiler hashes the source-cased property name
        // (SymbolExtensions.GetTranslationName returns property.Name); on newer runtimes it uses the
        // canonical PropertyKind name. LC0091 must mirror this gate — an unconditional canonical
        // override would produce false positives (or false negatives) on legacy apps whose XLIFF
        // uses source-cased hashes. Both tests use a single-field page with source-cased "Tooltip"
        // and an app.json declaring runtime 5.1, and paired XLIFFs designed to fail under the buggy
        // unconditional-override behavior.
        [Test]
        [TestCase("LegacyRuntimePageControlToolTip")]
        public async Task HasDiagnosticLegacyRuntime(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(HasDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            // XLIFF contains only the canonical hash (1295455071). With the runtime gate correctly
            // disabling the canonical override on runtime 5.1, the analyzer hashes source-cased
            // "Tooltip" to 2001309823, which is NOT in the XLIFF -> diagnostic fires as expected.
            // Under the buggy unconditional-override path, the analyzer would match the canonical
            // trans-unit and silently miss the diagnostic (false negative regression).
            var fixture = CreateFixtureWithLegacyRuntime(LegacyCanonicalTooltipXliffContent);
            fixture.HasDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }

        [Test]
        [TestCase("LegacyRuntimePageControlToolTip")]
        public async Task NoDiagnosticLegacyRuntime(string testCase)
        {
            RequireMinimumVersion("16.0",
                "LC0091 requires net8.0 SDK APIs (ExtensionObjectFoldingUtilities, GetLabelTextConstLanguageSymbolId)");

            var code = await File.ReadAllTextAsync(
                Path.Combine(_testCasePath, nameof(NoDiagnostic), $"{testCase}.al"))
                .ConfigureAwait(false);

            // XLIFF contains only the source-cased hash (2001309823). With the runtime gate
            // correctly disabling the canonical override on runtime 5.1, the analyzer hashes to
            // the same source-cased hash and matches the trans-unit -> no diagnostic. Under the
            // buggy unconditional-override path, the analyzer would hash to 1295455071 and NOT
            // match, raising a false-positive diagnostic on a fully-translated property.
            var fixture = CreateFixtureWithLegacyRuntime(LegacySourceCasedTooltipXliffContent);
            fixture.NoDiagnosticAtAllMarkers(code, DiagnosticIds.TranslatableTextShouldBeTranslated);
        }
    }
}
