const app = Vue.createApp({});

app.component('video-feed', {
    props: {
        cameraId: String,
        cameraName: String,
        hubUrl: String,
    },
    template: `
        <div class="cambox">
            <h3>{{ cameraName }}</h3>
            <canvas :ref="cameraId" width="1280" height="720"></canvas>
        </div>
    `,

    //< audio : ref = "'audio_' + cameraId" src = "/audio/human_detected.wav" ></audio >

    mounted() {
        //:width="width" :height="height"
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

        connection.on("ReceiveForwardedMessage", data => {
            this.displayFrame(data, img);
        });

        // Listen for person detection event
        //connection.on("HumanDetected", (cameraId) => {
        //    if (cameraId === this.cameraId) {
        //        const audio = this.$refs['audio_' + cameraId];
        //        if (audio) {
        //            audio.currentTime = 0;
        //            audio.play();
        //        }
        //    }
        //});

        connection
            .start()
            .then(() => {
                //connection.invoke("JoinGroup", `camera_${this.cameraId}_human_detected`);
                connection.invoke("JoinGroup", `camera_${this.cameraId}`);
                return

            })
            .catch(err => console.error("SignalR Error:", err.toString()));
    }
});

app.mount("#camsContainer");