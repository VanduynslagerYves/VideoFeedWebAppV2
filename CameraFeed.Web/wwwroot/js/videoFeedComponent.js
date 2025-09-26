const app = Vue.createApp({});

app.component('video-feed', {
    props: {
        cameraId: String,
        cameraName: String,
        localHubUrl: String,
        remoteHubUrl: String,
    },
    template: `
        <div class="cambox">
            <h3>{{ cameraName }}</h3>
            <canvas :ref="cameraId" width="1280" height="720"></canvas>
        </div>
    `,

    mounted() {
        const canvas = this.$refs[this.cameraId];
        const ctx = canvas.getContext('2d');
        const img = new Image(); // Reuse a single Image instance to prevent memory leaks

        let latestData = null;
        let loading = false;
        let connection = null;

        img.onload = () => {
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.drawImage(img, 0, 0);
            loading = false;

            // If a new frame arrived while loading, display it now
            if (latestData) {
                const nextData = latestData;
                latestData = null;
                this.displayFrame(nextData, img);
            }
        };

        this.displayFrame = (data, imageObj) => {
            if (loading) {
                latestData = data;
                return;
            }
            loading = true;
            imageObj.src = "data:image/jpeg;base64," + data;
        };

        // Helper to start connection and handle fallback
        const startConnection = async (primaryUrl, fallbackUrl) => {

            // Fetch connection info from secured endpoint
            const response = await fetch('/api/signalr/connection-info', { credentials: 'include' });
            if (!response.ok) {
                console.error("Failed to get SignalR connection info");
                return;
            }
            const token = response.text();

            connection = new signalR.HubConnectionBuilder()
                .withUrl(primaryUrl, {
                    accessTokenFactory: () => token
                }).build();

            connection.on("ReceiveForwardedMessage", data => {
                this.displayFrame(data, img);
            });

            connection.start()
                .then(() => {
                    connection.invoke("JoinGroup", `${this.cameraName}`);
                    connection.invoke("StartStreaming", `${this.cameraName}`);
                })
                .catch(() => {
                    if (fallbackUrl) {
                        // Retry once with remote URL
                        console.warn("Trying remote URL:", fallbackUrl);
                        startConnection(fallbackUrl, null);
                    }
                });

            // Stop streaming on page unload
            window.addEventListener("beforeunload", () => {
                connection.invoke("StopStreaming", `${this.cameraName}`)
                    .catch(() => { }); // Ignore errors if connection is already closed
            });

            // Stop streaming on connection close
            connection.onclose(() => {
                connection.invoke("StopStreaming", `${this.cameraName}`)
                    .catch(() => { }); // Ignore errors if connection is already closed
            });
        };

        startConnection(this.localHubUrl, this.remoteHubUrl);
    }
});

app.mount("#camsContainer");