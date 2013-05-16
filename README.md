# Introduction
The [SpatialKey Data Management REST API](http://support.spatialkey.com/dmapi) allows developers to programmatically create and update CSV data or Shapefiles in SpatialKey without having to login.  With the Command Line Tool, typical users of SpatialKey can leverage this functionality without any programming knowledge.  You just need to learn a few of the basics and you will be all set to kicking off import and update jobs outside of SpatialKey.

This documentation can also be found [here](http://support.spatialkey.com/data-management-api-command-line-tool/)

Note that the Data Management API Command Line Tool isn’t currently supported when called through a proxy server.

# Installing the Command Line Tool
1. Download the Command Line Tool and sample data [Download .zip file](https://github.com/SpatialKey/skdm/raw/master/skdm.zip)
2. Unzip contents to an accessible location - we will need to connect to that location later.  For example, I placed my unzipped folder at c:\program files\skdm.

You can optionally connect to the location of the files and view the help command now.
3. Open command-line prompt and go to directory above (optional)
4. Run "skdm.exe" to see the help command (optional)


###Command Line Tool files:
- skdm.exe (executable file to trigger import or update task)
- SpatialKeyDataManagerConfig.xml (XML descriptor for executable file)
- ICSharpCode.SharpZipLib.dll (code that .exe file uses)

###Sample Data files:
- 110thCongressionalDistrictShapefile.zip (sample Shapefile)
- SalesData.csv (Sample CSV file)
- SalesData.xml (XML descriptor for CSV file import) – for tips on generating an XML descriptor file for your CSV, check out this [article](http://support.spatialkey.com/dmapi-generate-xml)

# Setting up the Data Manager Config XML file
The Data Manager Config XML file is split into the following sections:
- Define Organization
- Authentication
- Action

See the sample SpatialKeyDataManagerConfig.xml shipped with the application for the proper xml format.

If you know XML, these sections will be easy to identify – if you don’t know XML, be patient and search through the file until you find these sections and then plug in the required information.  If you need an XML refresher, visit [W3Schools XML Tutorial](http://www.w3schools.com/xml/default.asp).

## Define Organization
When you define your organization in the XML file, you are telling the Data Import API where to send your data.  You can define your organization in a couple of ways: Cluster Domain URL or Organization Name.

### Cluster Domain URL
> XML File Default: `<clusterDomainUrl>http://xxx.spatialkey.com/</clusterDomainUrl>`  
> XML with my SpatialKey URL entered: `<clusterDomainUrl>http://demo.spatialkey.com/</clusterDomainUrl>`

You can comment out the Organization Name from the XML if you define your organization using the Cluster Domain URL.  See "conflicts" section below for details on why commenting out is suggested.

Finished section for my organization would look like this:
>     <!– <organizationName>xxx</organizationName> –>
>     <clusterDomainUrl>http://demo.spatialkey.com/</clusterDomainUrl>

### Organization Name
If you use this option, enter your SpatialKey Organization Name into the placeholder in the Config XML file.

> XML File Default: `<organizationName>xxx</organizationName>`  
> XML with my SpatialKey Organization Name entered: `<organizationName>Demo</organizationName>`

You can comment out the Cluster Domain URL from the XML if you define your organization using the Organization Name.  See "conflicts" section below for details on why commenting out is suggested.

Finished section for my organization would look like this:
>     <organizationName>Demo</organizationName>
>     <!– <clusterDomainUrl>xxx.spatialkey.com/</clusterDomainUrl> –>

## Authentication
When you authenticate in the XML file, you are telling the Data Import API who you are and giving it a chance to validate your permissions for the defined organization.  You can authenticate in a couple of ways: Authenticate with Keys or Authenticate with username/password.

### Authenticate with Keys
To authenticate with keys, you need to generate an API Key and retrieve your user ID from within SpatialKey.  Login to SpatialKey, click on “People” tab, and go into the settings for your user.  On this screen, you can view your user id and generate an API key.  Plug these values into the XML placeholders in the Config XML file.

> Default apiKey in XML file: `<apiKey>xxx</apiKey>`  
> XML file with my apiKey entered: `<apiKey>8b08e-fcb-42-93-828a2a2</apiKey>`  
> Default userId in XML file: `<userId>xxx</userId>`  
> XML file with my userId entered: `<userId>8afd7600ce0a462ad0d8a</userId>`

You can comment out the username/password from the XML if you authenticate using keys.  See "conflicts" section below for details on why commenting out is suggested.

Finished section for my organization would look like this:

>     <apiKey>8b08e-fcb-42-93-828a2a2</apiKey>
>     <userId>8afd7600ce0a462ad0d8a</userId>
>     <!–
>       <userName>user@xxx.com</userName>
>       <password>xxx</password>
>     –>

### Authenticate with Username/Password
To authenticate with username/password, enter your username and password combination into the Config XML file.

> Default userName in XML file: `<userName>user@xxx.com</userName>`  
> XML file with my userName: `<userName>user.name@spatialkey.com</userName>`

> Default password in XML file: `<password>xxx</password>`  
> XML file with my password: `<password>MyTempPass99</password>`

You can comment out the keys from the XML if you authenticate using username/password.  See "conflicts" section below for details on why commenting out is suggested.

Finished section for my organization would look like this:

>     <!–
>       <apiKey>xxx</apiKey>
>       <userId>xxx</userId>
>     –>
>     <userName>user.name@spatialkey.com</userName>
>     <password>MyTempPass99</password></code>

## Action
The “actions” section of the Config XML file defines all the actions that will be done when the Command Line Tool is executed.

The “type” element defines what action will be done.  The current options are:
- overwrite – create or overwrite an existing CSV dataset
- append – create or Append to an existing CSV dataset
- poly – create or overwite a shapefile

### Overwrite or Append Action
The overwrite and append actions will allow you to create, append to or overwrite an existing CSV dataset.  For both of these actions, the action section in the XML file is defined similarly:
- action name – the name you will specify when running the executable if you want to run only this action
- type – either overwrite or append
- dataPath – the path (relative to the config.xml) of the CSV file to upload
- xmlPath – the path (relative to the config.xml) to the CSV definition XML
- runAsBackground – true if you want to run the import in the background and return immediately without waiting for completion – defaults to “true”
- notifyByEmail – true if you want the authenticated user to be notified on completion – defaults to “true”
- addAllUsers – true if you want all users in your organization to have access to this dataset once imported into SpatialKey – defaults to “false”

Let’s set up an overwrite action for the sample CSV file provided in the SKDM folder.
>     <action name=”csv example”>
>       <type>overwrite</type>
>       <dataPath>SalesData.csv</dataPath>
>       <xmlPath>SalesData.xml</xmlPath>
>       <runAsBackground>true</runAsBackground>
>       <notifyByEmail>true</notifyByEmail>
>       <addAllUsers>false</addAllUsers>
>     </action>

### Poly Action
The poly action will allow you to create or overwrite a shapefile.  The poly action in the XML file is slightly  different depending on whether you are creating a new shapefile or updating an existing.

Note that you don't get any email notifications for shape notifications at this time.

*Create new shapefile:*
- action name - the name you will specify when running the executable if you want to run only this action
- type – poly
- dataPath –  the path (relative to the config.xml) of the shapefile – the shape file must be a zip
- datasetName – the name of the new shapefile
- datasetId – this field is ignored when a datasetName is defined, it can be removed or commented out when a datasetName is provided

*Overwrite existing shapefile:*
- action name - the name you will specify when running the executable if you want to run only this action
- type – poly
- dataPath –  the path (relative to the config.xml) of the shapefile – the shape file must be a zip
- datasetName – comment out or remove this line item completely when doing an overwrite of an existing Shapefile, if you leave it in, a new shapefile will be created
- datasetId – the id of the existing dataset to be overwritten
Note that in the Config XML file, the poly action is commented out.  Be sure to remove comments `<!--` and `–>` if you plan to use this action.  Let’s set up this action for both creating a new shapefile and overwriting and existing shapefile with the sample shapefile provided in the SKDM folder.

*Create new shapefile example:*
>     <action name=”shape example”>
>       <type>poly</type>
>       <dataPath>110thCongressionalDistrictShapefile.zip</dataPath>
>       <datasetName>110th Congressional District</datasetName>
>       <!– <datasetId>xxx</datasetId> –>
>     </action>

*Overwrite an existing shapefile example:*
>     <action name=”shape example”>
>       <type>poly</type>
>       <dataPath>110thCongressionalDistrictShapefile.zip</dataPath>
>       <!– <datasetName>110th Congressional District</datasetName> –>
>       <datasetId>8ab3d821d013ed04</datasetId>
>     </action>

### Another thing to consider when defining actions…
If you are creating a Config XML file that has many actions and you want to create an exception to the main file’s organizationName, clusterDomainUrl, apiKey, userId, username, or password, you can do so by adding a command for a specific action.

Simply add a line item (or multiple) in the action
>     <action name=”csv example”>
>       <clusterDomainUrl>MyOtherURL.spatialkey.com/</clusterDomainUrl>
>       <type>overwrite</type>
>       <dataPath>SalesData.csv</dataPath>
>       <xmlPath>SalesData.xml</xmlPath>
>       <runAsBackground>true</runAsBackground>
>       <notifyByEmail>true</notifyByEmail>
>     </action>

See "conflicts" section for some important considerations.

## Conflicts
Here are a couple of conflicts to remember when setting up your Config XML file.
- When both Cluster Domain URL and the Organization Name are defined, Cluster Domain URL will be used.  This is why it was suggested that one or the other should be commented out.
- When both Keys and username/password are provided for authentication, they keys will be used.  This is why it was suggested that one of the other should be commented out.

When creating exceptions to the main file’s organizationName, clusterDomainUrl, apiKey/userId, or username/password within a specific action, keep the above conflicts in mind.  As an example, if you define the clusterDomainUrl in the main body of the Config XML file and you enter the organizationName in the action as a exception, the clusterDomainUrl will still be used because it wins the conflict.  In order to completely override to originally defined clusterDomainURL in this case, you will have to define the organizationName and enter a blank clusterDomainURL in the action.

> Defined in body for Config XML file: `<clusterDomainURL>demo.spatialkey.com</clusterDomainURL>`  
> Defined in “action” for Config XML file:

>     <organizationName>Demo</organizationName>
>     <clusterDomainURL></clusterDomainURL>

# Running the Data Management API Command Line Tool
Now for the easy part.  Open command-line prompt and navigate to the SKDM directory where you put the unzipped folder.  Run “skdm.exe SpatialKeyDataManagerConfig.xml” – you can optionally specify a list of actions to perform.  When no actions are specified, all actions from the Config XML file will be run by default.

Running Data Management API Command Line Tool from a **Windows** workstation?

Command to run all actions:
> `skdm.exe SpatialKeyDataManagerConfig.xml`

Command to run only the “shape example” action:
> `skdm.exe SpatialKeyDataManagerConfig.xml “shape example”` (add quotes if there is a space in the action name)

Running Data Management API Command Line Tool from a **Mac** workstation?  Install on your workstation. ([Installation Instructions](http://www.mono-project.com/Mono:OSX))

Command to run all actions:
> `mono skdm.exe SpatialKeyDataManagerConfig.xml`

Command to run only the “shape example” action:
> `mono skdm.exe SpatialKeyDataManagerConfig.xml “shape example”` (add quotes if there is a space in the action name)