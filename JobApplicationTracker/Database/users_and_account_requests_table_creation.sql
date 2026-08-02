CREATE TABLE users (
	user_id INT NOT NULL AUTO_INCREMENT,
	first_name VARCHAR(25) NOT NULL,
	last_name VARCHAR(25) NOT NULL,
	email VARCHAR(50) NOT NULL,
	username VARCHAR(25) NOT NULL,
	pass VARCHAR(50) NOT NULL,
	role VARCHAR(20) NOT NULL DEFAULT 'user',
	active_status BOOLEAN NOT NULL DEFAULT TRUE,
	created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
		ON UPDATE CURRENT_TIMESTAMP,

	PRIMARY KEY (user_id),
	UNIQUE KEY unique_users_email (email),
	UNIQUE KEY unique_users_username (username)
);

CREATE TABLE account_requests (
    request_id INT NOT NULL AUTO_INCREMENT,
    first_name VARCHAR(25) NOT NULL,
    last_name VARCHAR(25) NOT NULL,
    email VARCHAR(50) NOT NULL,
    username VARCHAR(25) NOT NULL,
    pass VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    requested_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (request_id)
);

CREATE TABLE job_applications (
    application_id INT NOT NULL AUTO_INCREMENT,
    user_id INT NOT NULL,
    company VARCHAR(100) NOT NULL,
    job_title VARCHAR(100) NOT NULL,
    date_applied DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Applied',
    archived BOOLEAN NOT NULL DEFAULT FALSE,

    PRIMARY KEY (application_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);