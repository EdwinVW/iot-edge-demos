var visualization = (function () {

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

    function displayNotification(notification) {
        var sensorId = parseInt(notification.SensorId);
        var customerCount = parseInt(notification.CustomerCount);
        var maxCapacity = parseInt(notification.MaxCapacity);
        doorSensors[sensorId].customerCount = customerCount;
        doorSensors[sensorId].maxCapacity = maxCapacity;
        doorSensors[sensorId].storeStatus = notification.StoreStatus;
        flashDoorSensor(doorSensors[sensorId], notification.NotificationType);
        updateCustomerGraph();
    }

    function loadMallMap() {
        var img = new Image();
        img.onload = function () {
            mallMapContext.drawImage(img, 0, 0);
            showDoorSensors();
        }
        img.src = "img/mall-map.png";
    };

    function showDoorSensors() {
        doorSensors.forEach(function (sensor) {
            drawDoorSensor(sensor.x, sensor.y, sensorSize, sensor.storeStatus == 0 ? sensorIdleColorClosed : sensorIdleColorOpen, true);   
            mallMapContext.fillStyle = "#000000";
            mallMapContext.fillText(sensor.id, sensor.x - 8, sensor.y + 16);
        });
    }

    function flashDoorSensor(sensor, notificationType) {
        var flashColor;
        switch(notificationType) {
            case 0: // CustomerEntered
                flashColor = sensorInColor;
              break;
            case 1: // CustomerExited
                flashColor = sensorOutColor;
              break;
            case 2: // StoreClosed
                flashColor = sensorIdleColorClosed;
        }

        drawDoorSensor(sensor.x, sensor.y, notificationSize, flashColor, false);

        var idleColor = sensor.storeStatus == 0 ? sensorIdleColorClosed : sensorIdleColorOpen;
        setTimeout(function () {
            drawDoorSensor(sensor.x, sensor.y, notificationSize, idleColor, false);
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
        var x = sensorGraphLeft;
        customerGraphContext.clearRect(0, 0, 1222, 250);
        customerGraphContext.strokeStyle = "#000000";
        customerGraphContext.lineWidth = 0.25;
        doorSensors.forEach(function (sensor) {
            customerGraphContext.fillText(sensor.id, x + 4, 190);
            customerGraphContext.strokeRect(x, 10, sensorGraphBarWidth, 160);
            x += sensorGraphBarWidth + sensorGraphBarGap;
        });
    }

    function updateCustomerGraph() {
        var x = sensorGraphLeft;
        doorSensors.forEach(function (sensor) {
            customerGraphContext.clearRect(x + 1, 11, sensorGraphBarWidth - 2, 159);
            var barLength = parseInt((sensor.customerCount / sensor.maxCapacity) * 159);
            var percentage = parseInt((sensor.customerCount / sensor.maxCapacity) * 100);
            var y = 159 - barLength;
            customerGraphContext.fillStyle = perc2Color(percentage);
            customerGraphContext.fillRect(x + 1, y + 11, sensorGraphBarWidth - 2, barLength);
            x += sensorGraphBarWidth + sensorGraphBarGap;
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

    function updateTotalCustomerCountChart() {
        var totalCustomerCountChart = new Chart(totalGraphContext, {
            type: 'line',
            data: {
                datasets: [{
                    label: 'Total customer-count',
                    data: [],            
                    backgroundColor: ['rgba(0, 99, 132, 0.2)'],
                    borderColor: ['rgba(0,99,132,1)'],
                    borderWidth: 2,
                    fill: true,
                    pointRadius: 3                    
                }]
            },
            options: {
                legend: {
                    display: false
                },
                tooltips: {
                    enabled: false
                },
                layout: {
                    padding: {
                        left: 50
                    }
                },
                scales: {
                    xAxes: [{
                        type: 'realtime',       // x axis will auto-scroll from right to left
                        realtime: {             // per-axis options
                            duration: 50000,    // data in the past x ms will be displayed
                            refresh: 2000,      // onRefresh callback will be called every x ms
                            delay: 2000,        // delay of x ms, so upcoming values are known before plotting a line
                            pause: false,       // chart is not paused
                            ttl: 3600000,       // data will be automatically deleted as it disappears off the chart
                            onRefresh: function (chart) {
                                var totalCustomerCount = 0;
                                doorSensors.forEach(function (sensor) {
                                    totalCustomerCount += sensor.customerCount;
                                });                             
                                var data = [{ x: Date.now(), y: totalCustomerCount }];
                                Array.prototype.push.apply(chart.data.datasets[0].data, data);
                            }
                        }
                    }],
                    yAxes: [{
                        ticks: {
                            suggestedMax: 25,
                            beginAtZero:true
                        }
                    }]
                },
                plugins: {
                    streaming: {            // per-chart option
                        frameRate: 20       // chart is drawn x times every second
                    }
                }
            }
        });
    }

    function init() {
        loadMallMap();
        initializeCustomerGraph();
        updateCustomerGraph();
        updateTotalCustomerCountChart();
    };

    // expose functions
    return {
        init: init,
        displayNotification: displayNotification
    };
})();