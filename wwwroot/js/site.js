@section Scripts {
    <script>
        let selectedShowtimeId = null;
        function openShowtimeModal(movieId, movieTitle) {
            document.getElementById('modalMovieTitle').innerText = movieTitle;
        document.getElementById('showtimeList').innerHTML = 'Loading...';
        document.getElementById('confirmShowtimeBtn').disabled = true;
        selectedShowtimeId = null;
        var modal = new bootstrap.Modal(document.getElementById('showtimeModal'));
        modal.show();

        fetch('/ShowTime/GetByMovie?movieId=' + movieId)
    .then(response => response.json())
    .then(data => {
            let html = '';
        data.forEach(function(st){
            html += `<div>
                        <input type="radio" name="showtime" value="${st.id}" id="showtime${st.id}">
                        <label for="showtime${st.id}">${st.startTime} - ${st.roomName}</label>
                     </div>`;
        });
        document.getElementById('showtimeList').innerHTML = html;
        document.querySelectorAll('input[name="showtime"]').forEach(function(el){
            el.addEventListener('change', function () {
                selectedShowtimeId = this.value;
                document.getElementById('confirmShowtimeBtn').disabled = false;
            });
        });
    });
}

        document.getElementById('confirmShowtimeBtn').onclick = function(){
    if(selectedShowtimeId){
            window.location.href = '/Booking/SelectSeat/' + selectedShowtimeId;
    }
}
    </script>
}