import http from 'k6/http';
import { sleep } from 'k6';

export default function () {
    var dateString = (new Date()).getTime();
    // Replace with your target URL
    let url = `http://${__ENV.PUBLIC_IP}/api/AsyncViaQueue?query=${dateString}`;

    // Make a GET request to the URL
    let response = http.get(url);

    // Log the response status (optional)
    console.log(`Calling http://${__ENV.PUBLIC_IP}/api/AsyncViaQueue: ${response.status}:\n${response.body}`);

    // Pause for 2 second between requests (optional)
    sleep(10);
}