const app = Vue.createApp({});

app.component('video-feed', {
    props: {
        cameraId: String,
        hubUrl: String,
    },
    template: `
        <div class="cambox">
            <h3>Camera {{ cameraId }}</h3>
            <canvas :ref="cameraId" width="1920" height="1080"></canvas>
        </div>
    `,
    mounted() {
        const canvas = this.$refs[this.cameraId];
        const ctx = canvas.getContext('2d');
        const img = new Image(); // Reuse a single Image instance to prevent memory leaks

        let latestData = null;
        let loading = false;

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

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .build();

        connection.on("ReceiveImgBytes", data => {
            this.displayFrame(data, img);
        });

        connection
            .start()
            .then(() => {
                return connection.invoke("JoinGroup", `camera_${this.cameraId}`);
            })
            .catch(err => console.error("SignalR Error:", err.toString()));
    }
});

app.mount("#camsContainer");