CREATE TABLE applications (
    application_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    company_name VARCHAR(100) NOT NULL,
    job_title VARCHAR(100) NOT NULL,
    location VARCHAR(100),
    date_applied DATE NOT NULL,
    application_status VARCHAR(20)
        NOT NULL
        DEFAULT 'Applied',
    job_posting_url VARCHAR(500),
    follow_up_date DATE,
    notes TEXT,
    is_archived TINYINT(1)
        NOT NULL
        DEFAULT 0,
    created_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_applications_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE documents (
    document_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    document_type VARCHAR(20) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size INT NOT NULL,
    file_data MEDIUMBLOB NOT NULL,
    notes TEXT,
    uploaded_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_documents_user
        FOREIGN KEY (user_id)
        REFERENCES users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE recruiters (
    recruiter_id INT AUTO_INCREMENT PRIMARY KEY,
    application_id INT NOT NULL,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    company_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL,
    phone VARCHAR(25),
    follow_up_reminder_date DATE,
    last_contact_date DATE,
    notes TEXT,
    created_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP
        ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_recruiters_application
        FOREIGN KEY (application_id)
        REFERENCES applications(application_id)
        ON DELETE CASCADE
);

CREATE TABLE application_documents (
    application_id INT NOT NULL,
    document_id INT NOT NULL,
    application_notes TEXT,
    linked_at TIMESTAMP
        NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (
        application_id,
        document_id
    ),

    CONSTRAINT fk_application_documents_application
        FOREIGN KEY (application_id)
        REFERENCES applications(application_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_application_documents_document
        FOREIGN KEY (document_id)
        REFERENCES documents(document_id)
        ON DELETE CASCADE
);