const app = Vue.createApp({});

app.component('video-feed', {
    props: {
        cameraId: String,
        hubUrl: String,
    },
    template: `
        <div>
            <h3>Camera: {{ cameraId }}</h3>
            <canvas :ref="cameraId" width="800" height="600"></canvas>
        </div>
    `,
    mounted() {
        const canvas = this.$refs[this.cameraId];
        const ctx = canvas.getContext('2d');

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .build();

        connection.on("ReceiveFrame", data => {
            const img = new Image();
            img.onload = () => {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(img, 0, 0);
            };
            img.src = "data:image/jpeg;base64," + data;
        });

        connection
            .start()
            .then(() => {
                return connection.invoke("JoinGroup", `camera_${this.cameraId}`);
            })
            .catch(err => console.error("SignalR Error:", err.toString()));
    }
});

app.mount("#app");
