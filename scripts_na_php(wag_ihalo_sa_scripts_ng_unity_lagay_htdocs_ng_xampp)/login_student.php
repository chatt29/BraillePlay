<?php
$conn = new mysqli("localhost", "root", "", "brailleplay");

$username = isset($_POST['username']) ? $_POST['username'] : '';
$password = isset($_POST['password']) ? $_POST['password'] : '';

$stmt = $conn->prepare("SELECT * FROM student WHERE username=?");
$stmt->bind_param("s", $username);
$stmt->execute();

$result = $stmt->get_result();

if ($result->num_rows > 0) {
    $row = $result->fetch_assoc();

    if (password_verify($password, $row['password'])) {
        echo "Success|" . $row['student_id'] . "|" . $row['first_name'];
    } else {
        echo "Wrong Password";
    }
} else {
    echo "User Not Found";
}

$conn->close();
//syempre para mag login
?>