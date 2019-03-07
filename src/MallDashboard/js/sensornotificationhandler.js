var notificationHandler = (function () {

    "use strict";
    
    var mallMapContext = document.getElementById('mallMap').getContext("2d");
    mallMapContext.font = "10px Arial";
    var customerGraphContext = document.getElementById('customerGraph').getContext("2d");
    customerGraphContext.font = "10px Arial";
    var totalGraphContext = document.getElementById("totalGraph").getContext("2d");

    const sensorSize = 6;
    const notificationSize = 5;
    const sensorIdleColorOpen = "#00BBFF";
    const sensorIdleColorClosed = "#001111";
    const sensorInColor = "#00FF00";
    const sensorOutColor = "#FF0000";
    const sensorGraphLeft = 60;
    const sensorGraphBarWidth = 15;
    const sensorGraphBarGap = 12;

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