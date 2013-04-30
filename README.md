# Introduction
The Spatial Key Data Manager is used import and update new CSV and Shape datasets into your account on Spatial Key.  For more information about the API, please see:
http://support.spatialkey.com/dmapi

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
Set <organizationName> and comment out <clusterDomainUrl>
ex: <code><organizationName>xxx</organizationName></code>

### Organization Name
Set <clusterDomainUrl> and comment out <organizationName>
ex: <code><clusterDomainUrl>http://xxx.spatialkey.com/</clusterDomainUrl></code>

## Authentication
You can authenticate using either Keys or Username/Password.

These settings can be defined globally and overriden for each action.

### Authenticate with Keys
You can set the authenticaion keys globally and overide for each action.

1. Get your API Key and User ID from the SK Client
2. Set the <apiKey> and <userId> in your config.xml file.  Make sure to comment out <userName> and <password>

ex:<code>
	<apiKey>xxx</apiKey>
	<userId>yyy</userId>
</code>

### Authenticate with Username/Password
You can set the username/password globally and overide for each action.

Set the <userName> and <password> in your config.xml file.  Make sure to comment out <apiKey> and <userId>

ex:<code>
	<userName>myusername@someplace.com</userName>
	<password>xxx</password>
</code>

## Action List
The <actions> section of the config.xml defines all the actions that will be done.  Currently you can upload/update CSV and Shape datafiles.

Each action has a name that can be used to invoke the config to run just the given name(s).

Each action can override the Organization and Authenticaion being used.

The <type> element defines what action will be done.  The current actions are:
* overwrite - Create or overwrite and existing CSV datafile.
* append - Create or Append to an existing CSV datafile.
* poly - Create or overwite a shape file.

### Action Type: overwrite

### Action Type: append

### Action Type: poly

# Running SKDM

1. Open command-line prompt and go to SKDM directory
2. Run "skdm.exe <config.xml>" optionally add a list of actions to perform; the default is to run them all.

ex: <code>skdm.exe SpatialKeyDataManagerConfig.xml</code>
ex: <code>skdm.exe SpatialKeyDataManagerConfig.xml action1 action7</code>
