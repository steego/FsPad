<script lang="ts">
	import Viewer from "./Viewer.svelte";
	import 'bulma/css/bulma.css'
	import { Button } from 'svelma'

	export let object: unknown = "Not connected...";

	init();

	function init() {
		const output = document.getElementById("output");
		createSocket();
	}

	function createSocket() {
		// const port = "$$$PORT$$$"
		const port = "8080";
		var wsUri = `ws://localhost:${port}/websocket`;

		const websocket = new WebSocket(wsUri);
		websocket.onopen = function (evt) {
			onOpen(evt);
		};
		websocket.onclose = function (evt) {
			onClose(evt);
		};
		websocket.onmessage = function (evt) {
			onMessage(evt);
		};
		websocket.onerror = function (evt) {
			onError(evt);
		};

		function onOpen(evt) {
			var path = window.location.pathname;
			doSend("START: " + path);
		}

		function onClose(evt) {
			writeToScreen("DISCONNECTED");
			setTimeout(function () {
				init();
			}, 1000);
		}

		function onMessage(evt) {
			writeToScreen(evt.data);
		}

		function onError(evt) {
			object = evt;
			// writeToScreen(
			// 	'<span style=""color: red;"">ERROR:</span> ' + evt.data
			// );
		}

		function doSend(message) {
			websocket.send(message);
		}

		function writeToScreen(message) {
			try {
				object = JSON.parse(message);
			} catch(err) {
				object = {
					error: err.message,
					json: message
				}
			}
		}
	}
</script>

<main>
	<section class="section">

		<div class="container">
	
			<h1 class="title">Dump</h1>
			<Viewer value={object} />
		</div>
	</section>

	<!-- <Button>I am a Button</Button> -->
</main>
