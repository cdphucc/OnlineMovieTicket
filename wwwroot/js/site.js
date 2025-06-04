// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// wwwroot/js/showtimes-popup.js
document.addEventListener('DOMContentLoaded', function () {
    // Gán sự kiện cho các item chọn rạp
    document.querySelectorAll('.header-navigation-menu .dropdown-menu .a"]').forEach(function (item) {
        item.addEventListener('click', function (e) {
            e.preventDefault();
            var cinemaId = this.getAttribute('href').split('/').pop();
            fetch(`/ShowTimes/GetShowTimesByCinema?cinemaId=${cinemaId}`)
                .then(res => res.json())
                .then(data => {
                    let html = '';
                    if (data.length === 0) {
                        html = '<p>Hiện chưa có suất chiếu nào cho rạp này.</p>';
                    } else {
                        html = '<table class="table table-sm table-bordered"><thead><tr><th>Phim</th><th>Phòng</th><th>Giờ chiếu</th><th>Giá vé</th><th>Định dạng</th><th>Trạng thái</th></tr></thead><tbody>';
                        data.forEach(st => {
                            html += `<tr>
                                <td>${st.MovieTitle}</td>
                                <td>${st.RoomName}</td>
                                <td>${st.StartTime}</td>
                                <td>${st.Price.toLocaleString()}đ</td>
                                <td>${st.Format}</td>
                                <td>${st.Status}</td>
                            </tr>`;
                        });
                        html += '</tbody></table>';
                    }
                    document.getElementById('showtimes-content').innerHTML = html;
                    document.getElementById('showtimes-popup').style.display = 'block';
                });
        });
    });
    // Đóng popup
    document.getElementById('close-showtimes-popup').onclick = function () {
        document.getElementById('showtimes-popup').style.display = 'none';
    }
});