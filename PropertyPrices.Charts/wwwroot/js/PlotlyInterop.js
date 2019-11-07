window.PlotlyInterop = {

    darkLayout: function () {
        return {
            "margin": {
                "b": 40,
                "l": 60,
                "t": 25,
                "r": 10
            },
            "plot_bgcolor": "#000",
            "paper_bgcolor": "#000",
            "family": "Roboto, Helvetica, Arial, sans-serif",
            "font": { "color": "#fff" },
            "xaxis": {
                "domain": [0, 1],
                "automargin": true,
                "title": []
            },
            "yaxis": {
                "domain": [0, 1],
                "automargin": true,
                "title": []
            },
            "hovermode": "closest",
            "showlegend": true
        };

    },
    lightLayout: function () {
        return {
            "margin": {
                "b": 40,
                "l": 60,
                "t": 25,
                "r": 10
            },
            "plot_bgcolor": "#fff",
            "paper_bgcolor": "#fff",
            "font": {
                "family": "Roboto, Helvetica, Arial, sans-serif",
                "color": "#000"
            },
            "xaxis": {
                "domain": [0, 1],
                "automargin": true,
                "title": []
            },
            "yaxis": {
                "domain": [0, 1],
                "automargin": true,
                "title": []
            },
            "hovermode": "closest",
            "showlegend": true
        };

    },

    selectedLayout: function () {
        return $("#selectedLayout").val() === "darkLayout" ? this.darkLayout() : this.lightLayout();
    },

    toggleBackground: function () {
        return $("#selectedLayout").val() === "darkLayout" ? jQuery("body").css("background-color", "#000") : jQuery("body").css("background-color", "#fff");
    },

    newPlot: function (data) {
        //console.log(data);
        this.toggleBackground();
        Plotly.newPlot(document.getElementById("graph"), JSON.parse(data), PlotlyInterop.selectedLayout());

        return true;
    }
};