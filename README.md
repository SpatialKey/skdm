# Introduction
The Spatial Key Data Manager (SKDM) is used import and update new CSV and Shape datasets into your account on Spatial Key.  For more information about the API, please see:
http://support.spatialkey.com/dmapi

This guide and configuring the SKDM  assumes a working knowledge of XML.  If you need to learn XML, go to http://www.w3schools.com/xml/default.asp

# Installing SKDM
1. Download https://github.com/SpatialKey/skdm/raw/master/skdm.zip
2. Unzip contents to skdm directory
3. Open command-line prompt and go to directory above
4. Run "skdm.exe" to see the help command

# Setup Configuration
See the sample SpatialKeyDataManagerConfig.xml shipped with the applicaiton for the proper xml format.

## Define Organization
You can configure the application to use either a URL or Organization Name to connect to the correct server.  If you use Organization Name, a cluster lookup is performed to find the actual URL.

These settings can be defined globally and overriden for each action.

### Cluster Domain URL
Set  &lt;clusterDomainUrl> and comment out  &lt;organizationName>

ex: <code>&lt;clusterDomainUrl>http://xxx.spatialkey.com/&lt;/clusterDomainUrl></code>

### Organization Name
Set  &lt;organizationName> and comment out  &lt;clusterDomainUrl>

ex: <code>&lt;organizationName>xxx&lt;/organizationName></code>

## Authentication
You can authenticate using either Keys or Username/Password.

These settings can be defined globally and overriden for each action.

### Authenticate with Keys
You can set the authenticaion keys globally and overide for each action.

1. Get your API Key and User ID from the SK Client
2. Set the  &lt;apiKey> and  &lt;userId> in your config.xml file.  Make sure to comment out  &lt;userName> and  &lt;password>

ex:<code>&lt;apiKey>xxx&lt;/apiKey>&lt;userId>yyy&lt;/userId>
</code>

### Authenticate with Username/Password
You can set the username/password globally and overide for each action.

Set the  &lt;userName> and  &lt;password> in your config.xml file.  Make sure to comment out  &lt;apiKey> and  &lt;userId>

ex:<code>&lt;userName>myusername@someplace.com&lt;/userName>&lt;password>xxx &lt;/password></code>

## Action List
The  &lt;actions> section of the config.xml defines all the actions that will be done.  Currently you can upload/update CSV and Shape datafiles.

Each action has a name that can be used to invoke the config to run just the given name(s).

Each action can override the Organization and Authenticaion being used.

The  &lt;type> element defines what action will be done.  The current actions are:
* overwrite - Create or overwrite and existing CSV datafile.
* append - Create or Append to an existing CSV datafile.
* poly - Create or overwite a shape file.

### Action Type: overwrite and append
The overwrite and append actions use the same parameters.  The overwrite action will create or overwrite an existing CSV dataset while the append action will append to an existing dataset or create a new one.  The valid parameters are:
* dataPath - The path (rlative to the config.xml) of the CSV file to upload.
* xmlPath - The path (relative to the config.xml) to the CSV definition XML.  You can generate this XML file using the SpatialKey Client following these instructions http://support.spatialkey.com/dmapi-generate-xml
* runAsBackground - true if you ant to run the import in the background and return immediately without waiting for completion.  Defaults to "true"
* notifyByEmail - true if you want the authenticated user to be notified on completion.  Defaults to "true"
* addAllUsers - true if you want all users in your organization to have access to this file.  Defaults to "false"

### Action Type: poly
Used to create or overwrite a Shape dataset. The parameters for this action are:
* datasetName - The name of a new dataset.  If you pass in this parameter, the datasetId is ignored.
* datasetId - The id of an existing shape dataset to overwrite.
* datapath - The path to the shape file.  The shape file must be a zip.

# Running SKDM

1. Open command-line prompt and go to SKDM directory
2. Run "skdm.exe  &lt;config.xml>" optionally add a list of actions to perform; the default is to run them all.

ex: <code>skdm.exe SpatialKeyDataManagerConfig.xml</code>

ex: <code>skdm.exe SpatialKeyDataManagerConfig.xml action1 action7</code>
