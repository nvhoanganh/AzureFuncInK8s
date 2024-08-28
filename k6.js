import http from 'k6/http';
import { sleep } from 'k6';

export default function () {
    // Replace with your target URL
    const url = `http://${__ENV.PUBLIC_IP}/api/HttpExample`;

    // Make a GET request to the URL
    let response = http.get(url);

    // Log the response status (optional)
    console.log(`Status: ${response.status}:\n${response.body}`);

    // Pause for 1 second between requests (optional)
    sleep(1);
}