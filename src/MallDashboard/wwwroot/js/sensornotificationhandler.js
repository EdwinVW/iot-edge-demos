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
    const graphLeft = 200;

    var connection;

    function loadMallMap() {
        var img = new Image();
        img.onload = function () {
            mallMapContext.drawImage(img, 0, 0);
            showDoorSensors();
        }
        img.src = "img/mall-map.png";
    };

    function setupSignalR() {
        // signalRInfoKey variable must be created in a separate .js file called secrets.js and 
        // filled with the default function key that you can obtain from the Azure portal
        var signalRInfoUrl = "https://ew-iot.azurewebsites.net/api/SignalRInfo?code=" + signalRInfoKey;

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
        var sensorId = parseInt(notification.SensorId);
        var customerCount = parseInt(notification.CustomerCount);
        var maxCapacity = parseInt(notification.MaxCapacity);
        doorSensors[sensorId].customerCount = customerCount;
        doorSensors[sensorId].maxCapacity = maxCapacity;
        doorSensors[sensorId].storeStatus = notification.StoreStatus;
        flashDoorSensor(doorSensors[sensorId], notification.NotificationType);
        updateCustomerGraph();
    }

    function showDoorSensors() {
        doorSensors.forEach(function (sensor) {
            drawDoorSensor(sensor.x, sensor.y, sensorSize, sensor.storeStatus == 0 ? sensorIdleColorClosed : sensorIdleColorOpen, true);   
            mallMapContext.fillStyle = "#000000";
            mallMapContext.fillText(sensor.id, sensor.x - 5, sensor.y + 15);
        });
    }

    function flashDoorSensor(sensor, notificationType) {
        var flashColor = notificationType == 0 ? sensorInColor : sensorOutColor;
        drawDoorSensor(sensor.x, sensor.y, notificationSize, flashColor, false)
        setTimeout(function () {
            drawDoorSensor(sensor.x, sensor.y, notificationSize, sensor.storeStatus == 0 ? sensorIdleColorClosed : sensorIdleColorOpen, false);
        }, 250);
    }

    function drawDoorSensor(x, y, size, fillColor, stroke) {
        mallMapContext.beginPath();
        mallMapContext.arc(x, y, size, 0, 2 * Math.PI);
        mallMapContext.fillStyle = fillColor;
        mallMapContext.fill();
        if (stroke == true) {
            mallMapContext.stroke();
        }
    }

    function initializeCustomerGraph() {
        var x = graphLeft;
        customerGraphContext.clearRect(0, 0, 1222, 250);
        customerGraphContext.fillStyle = "#000000";
        customerGraphContext.fillText("Store:", 150, 190);
        customerGraphContext.strokeStyle = "#000000";
        customerGraphContext.lineWidth = 0.25;
        doorSensors.forEach(function (sensor) {
            customerGraphContext.fillText(sensor.id, x, 190);
            customerGraphContext.strokeRect(x, 10, 10, 160);
            x += 20;
        });
    }

    function updateCustomerGraph() {
        var x = graphLeft;
        doorSensors.forEach(function (sensor) {
            customerGraphContext.clearRect(x+ 1, 11, 8, 159);
            var barLength = parseInt((sensor.customerCount / sensor.maxCapacity) * 159);
            var percentage = parseInt((sensor.customerCount / sensor.maxCapacity) * 100);
            var y = 159 - barLength;
            customerGraphContext.fillStyle = perc2Color(percentage);
            customerGraphContext.fillRect(x + 1, y + 11, 8, barLength);
            x += 20;
        });
    }

    function perc2Color(perc) {
        var r, g, b = 0;
        if(perc < 50) {
            g = 255;
            r = Math.round(5.1 * perc);
        }
        else {
            r = 255;
            g = Math.round(510 - 5.10 * perc);
        }
        var h = r * 0x10000 + g * 0x100 + b * 0x1;
        return '#' + ('000000' + h.toString(16)).slice(-6);
    }

    function updateTotalChart() {
        var totalCustomerCountChart = new Chart(totalGraphContext, {
            type: 'line',               // 'line', 'bar', 'bubble' and 'scatter' types are supported
            data: {
                datasets: [{
                    label: 'Total customer-count',
                    data: [],            
                    backgroundColor: ['rgba(0, 99, 132, 0.2)'],
                    borderColor: ['rgba(0,99,132,1)'],
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 0
                }]
            },
            options: {
                legend: {
                    display: false
                },
                scales: {
                    xAxes: [{
                        type: 'realtime',        // x axis will auto-scroll from right to left
                        realtime: {              // per-axis options
                            duration: 50000,    // data in the past x ms will be displayed
                            refresh: 2000,       // onRefresh callback will be called every x ms
                            delay: 2000,         // delay of x ms, so upcoming values are known before plotting a line
                            pause: false,        // chart is not paused
                            ttl: 1800000,      // data will be automatically deleted as it disappears off the chart
                            // a callback to update datasets
                            onRefresh: function (chart) {
                                var totalCustomerCount = 0;
                                doorSensors.forEach(function (sensor) {
                                    totalCustomerCount += sensor.customerCount;
                                });                             
                                var data = [{ x: Date.now(), y: totalCustomerCount }];
                                Array.prototype.push.apply(chart.data.datasets[0].data, data);
                            }
                        }
                    }]
                },
                plugins: {
                    streaming: {            // per-chart option
                        frameRate: 30       // chart is drawn x times every second
                    }
                }
            }
        });
    }

    function start() {
        loadMallMap();
        initializeCustomerGraph();
        updateCustomerGraph();
        updateTotalChart();
        setupSignalR();
    };

    // expose start function
    return {
        start: start
    };

})();

notificationHandler.start();