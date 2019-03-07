var notificationHandler = (function () {

    "use strict";

    var connection;

    function setupSignalR() {
        // functionAppName and signalRInfoKey variable must be created in a separate .js 
        // file called secrets.js and filled with the default function app name and the 
        // key that you can obtain from the Azure portal
        var signalRInfoUrl = "https://" + functionAppName +".azurewebsites.net/api/SignalRInfo?code=" + signalRInfoKey;

        fetch(signalRInfoUrl)
            .then(function (response) {
                return response.json();
            })
            .then(function (data) {
                // get signalr options for authentication
                var connectionInfo = data;
                const options = { accessTokenFactory: () => connectionInfo.accessToken };

                // create signalR connection
                connection = new signalR.HubConnectionBuilder().withUrl(connectionInfo.url, options).build();

                // setup signalr callback
                connection.on("SendNotification", function (message) {
                    var notification = JSON.parse(message);
                    handleNotification(notification);
                });

                // start signalr connection
                connection.start().then(function () {
                    console.log("SignalR Connection started!");
                });
            })
            .catch(function () {
                console.log("Error fecthing connection-info.");
            });
    };

    function handleNotification(notification) {
        var id = notification.DeviceId + "/" + notification.ModuleId + "/Sensor" + notification.SensorId;
        console.info('Received notifcation from ' + id);

        visualization.displayNotification(notification);
    }

    function start() {
        visualization.init();
        setupSignalR();
    };

    // expose start function
    return {
        start: start
    };
})();

notificationHandler.start();