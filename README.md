#Data Management API Command Line Tool v2

This is the second release of the SpatialKey DM API command line tool. The initial release can be found at [here]https://github.com/SpatialKey/skdm/tree/v1

# Introduction
The [SpatialKey Data Management REST API](http://support.spatialkey.com/dmapi) allows developers to programmatically create and update CSV data or Shapefiles in SpatialKey without having to login.  With the Command Line Tool, typical users of SpatialKey can leverage this functionality without any programming knowledge.  You just need to learn a few of the basics and you will be all set to kicking off import and update jobs outside of SpatialKey.

This documentation can also be found [here](http://support.spatialkey.com/data-management-api-command-line-tool/)

Note that the Data Management API Command Line Tool isn’t currently supported when called through a proxy server.

# Installing the Command Line Tool
1. Download the Command Line Tool and sample data [Download .zip file](https://github.com/SpatialKey/skdm/blob/v2/skdm.zip)
2. Unzip contents to an accessible location - we will need to connect to that location later.  For example, I placed my unzipped folder at c:\program files\skdm.

You can optionally connect to the location of the files and view the help command now.
3. Open command-line prompt and go to directory above (optional)
4. Run "skdm.exe" to see the help command (optional)

# Exit Codes
 - 0 Success
 - 1 Warning
 - 2 Bad Command Line
 - 3 No Commands
 - 4 Error

# Options
## /help
> /help, -help, /h, -h, /?, -?

Show help message

## /config
> /config CONFIG_XML, -config, /c, -c

Data Manager Config XML file. (Default SpatialKeyDataManagerConfig.xml)

# /version
> /version, -version

Get application version

# /trace
> /trace LEVEL, -trace

Trace debug verbosity
 - The default of '0' shows errors and results only
 - '1' shows general status messages
 - '2' shows detailed debug version.
 
# Commands
## help
> help <command>

Show help for specific command.  Try '/help' for general help.

## oauth
> oauth [/ttl TTL] [ORG_API_KEY ORG_SECRET_KEY USER_API_KEY]

Get oAuth token for keys in XML configuration or by passing in values.  The organizationAPIKey, organizationSecretKey, or userAPIKey defined in the data manager config XML file can be overwritten by sending all three on the command line.
- /ttl TTL - sets the time to live for the token.  The default is 60 seconds.

## upload
> upload [/no-wait] [/keep-uploadid] [[ACTION1] ... [ACTIONN]]

Upload dataset data.  By default this waits until the import, append, or overwrite completes.  All actions are performed unless one or more specific actions are listed. 
- /no-wait - Doesn't wait for import, append, or overwrite to complete.  If this is defined, the datasetId created on imports will not be updated in the data manager config XML.
- /keep-uploadid - Don't cancel the upload id so it can be used in other manual operations.

## suggest
> suggest [[ACTION1] ... [ACTIONN]]

Get suggested config for data.  This will create an xml configuration for each action run.  All actions defined have their xml suggested unless specific actions are listed.   If the xml file defined by `<pathXML>` does not exist, the configuration will be written into that file name.  If the file does exist, the xml configuration will be written into a new file.

## list
> list

List available datasets

## delete
> delete ID [[ID] ... [ID]]

Delete datasets by id.  One or more ids can be specified.

###Command Line Tool files:
- skdm.exe (executable file to trigger import or update task)
- SpatialKeyDataManagerConfig.xml (XML descriptor for executable file)

###Sample Data files:
- 110thCongressionalDistrictShapefile.zip (sample Shapefile)
- 110thCongressionalDistrictShapefile.xml (sample Shapefile configuration)
- SalesData.csv (Sample CSV file)
- SalesData.xml (XML descriptor for CSV file import)

You can create the suggested xml configuration files by running "skdm.exe suggest".

For tips on generating an XML descriptor file for your CSV, check out this [article](http://support.spatialkey.com/xml-file-generation/)

# Setting up the Data Manager Config XML file
The Data Manager Config XML file is split into the following sections:
- Define Organization
- Define User
- Action

See the sample SpatialKeyDataManagerConfig.xml shipped with the application for the proper xml format.

If you know XML, these sections will be easy to identify – if you don’t know XML, be patient and search through the file until you find these sections and then plug in the required information.  If you need an XML refresher, visit [W3Schools XML Tutorial](http://www.w3schools.com/xml/default.asp).

## Define Organization
In order to authenticate to the api, you must first setup your organization URL. This should be the same URL you use to access the client up to and including ".spatialkey.com".  Make sure you use "https://".  For the "demo" organization the url would look like this:

>     <organizationURL>https://demo.spatialkey.com/</organizationURL>

Then you will need to get the organization API and secret key.  Login to SpatialKey, click on the "Admin" tab, and go to the "API Keys" settings.  Copy the "Public Key" into `<organizationApiKey>` and the "Secret Key" into `<organizationSecretKey>`:

>     <organizationAPIKey>1a2a3a-fcb-42-93-828a2a2</organizationAPIKey>
>     <organizationSecretKey>4b6b6b-fcb-42-93-828a2a2</organizationSecretKey>

## Define User
This field defines what user is authenticating into Spatial Key.  Login to SpatialKey, click on "People" tab, and go into the settings for your user.  On this screen, you can view your user id and generate an API key.  Copy the user "API Key" into the `<userAPIKey>`

>     <userAPIKey>7c8c9c-fcb-42-93-828a2a2</userAPIKey>

## Action
The "actions" section of the Config XML file defines all the actions that will be done when the Command Line Tool is executed.

The `<actionType>` element defines what action will be done.  The current options are:
- append – Append to an existing dataset.  If the `<datasetId>` is not set before running this action, an import will be done the first time it is run.  A shape dataset cannot be appended to, so this will overwrite.
- overwrite – Overwrite an existing dataset.  If the `<datasetId>` is not set before running this action, an import will be done the first time it is run.

- dataType - What type of data is in the `<pathData>`. Can be "CSV", "Shapefile", or "Insurance"
- pathData - Path of the data to upload.  CSV can be a raw file or zip, shapes are always zipped up.
- pathXML - configuration file for the data.  You can use the "suggest" command to get an xml file or retrieve it from an already uploaded sample on the Spatial Key client.
- datasetId (dataType not "Insurance") - datasetId to use for overwrite or append.  If not set, an import will be done and the generated id will be saved to the xml.
- insuranceId (dataType "Insurance") - insuranceId to use for overwrite or append.  If not set, an import will be done and the generated id will be saved to the xml.  A new insuranceId is always generated.

Note: you don't get email or client notifications at this time.

*Initial Import a Dataset*
>     <action name="csv example">
>       <actionType>overwrite or append</actionType>
>       <dataType>CSV</dataType>
>       <pathData>SalesData.csv</pathData>
>       <pathXML>SalesData.xml</pathXML>
>     </action>

*Overwrite Existing Shapefile*
>     <action name="csv example">
>       <actionType>overwrite</actionType>
>       <dataType>Shapefile</dataType>
>       <pathData>110thCongressionalDistrictShapefile.zip</pathData>
>       <pathXML>110thCongressionalDistrictShapefile.xml</pathXML>
>       <datasetId>a8a8a-848-33-33-283838</datasetId>
>     </action>

*Append To Existing Dataset*
>     <action name="csv example">
>       <actionType>overwrite</actionType>
>       <dataType>CSV</dataType>
>       <pathData>SalesData.csv</pathData>
>       <pathXML>SalesData.xml</pathXML>
>       <datasetId>1111-848-33-33-283838</datasetId>
>     </action>

### Another thing to consider when defining actions…
If you are creating a Config XML file that has many actions and you want to create an exception to the main file’s organizationURL, organizationAPIKey, organizationSecretKey, or userAPIKey, you can do so by adding a command for a specific action.

Simply add a line item (or multiple) in the action
>     <action name="csv example">
>       <organizationURL>https://xxx.spatialkey.com/</organizationURL>
>       <organizationAPIKey>xxx</organizationAPIKey>
>       <organizationSecretKey>xxx</organizationSecretKey>
>       <userAPIKey>xxx</userAPIKey>
>       <actionType>overwrite</actionType>
>       <dataType>CSV</dataType>
>       <pathData>SalesData.csv</pathData>
>       <pathXML>SalesData.xml</pathXML>
>       <datasetId>1111-848-33-33-283838</datasetId>
>     </action>


See "conflicts" section for some important considerations.

## Conflicts
When creating exceptions to the main file’s organizationName, organizationURL, organizationAPIKey, or userAPIKey within a specific action, remember that any not defined in the action will used the default ones.

If you are doing an overwrite or append but haven't defined a datasetId, the application will do an import instead.  If you defined /no-wait, the datasetId will not be updated.

# Running the Data Management API Command Line Tool
Now for the easy part.  Open command-line prompt and navigate to the SKDM directory where you put the unzipped folder.  Run "skdm.exe [options]".  When no commands are specified,the application will show help.

Running Data Management API Command Line Tool from a **Windows** workstation?

Syntax to run any command:
> `skdm.exe [options] <command> [<args>]`

Running Data Management API Command Line Tool from a **Mac** workstation?  Install on your workstation. ([Installation Instructions](http://www.mono-project.com/Mono:OSX))

Syntax to any command:
> `mono skdm.exe [options] <command> [<args>]`

## Examples
All of the samples use the **Windows** syntax.  Remember to run using `mono skdm.exe` instead of just `skdm.exe` on a **Mac** workstation.

Run all upload actions in SpatialKeyDataManagerConfig.xml
> `skdm.exe upload`

Run the specified upload action in SpatialKeyDataManagerConfig.xml
> `skdm.exe upload "sample csv"`

Run all upload actions in AnotherConfig.xml
> `skdm.exe /config AnotherConfig.xml upload`

Run suggest the default pathXML config for all actions in SpatialKeyDataManagerConfig.xml
> `skdm.exe suggest`

List all datasets for the organization and user defined in SpatialKeyDataManagerConfig.xml
> `skdm.exe list`

Delete the given dataset at the organization and user defined in SpatialKeyDataManagerConfig.xml
> `skdm.exe delete 55555-66-88-33-abcd`

Get oAuth key for organization and user defined in SpatialKeyDataManagerConfig.xml
> `skdm.exe oauth`
